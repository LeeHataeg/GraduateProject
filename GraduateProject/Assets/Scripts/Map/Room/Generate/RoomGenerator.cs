using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Define;
using Node = Define.MapNode;

// Convert SpaceArea into Real Room
public class RoomGenerator : MonoBehaviour
{
    private MapSO so;

    [SerializeField] private Grid grid; // 인스펙터 미할당 시 Awake에서 자동 탐색/생성

    private List<RoomInitData> roomDatas;
    public List<Room> rooms = new List<Room>();
    private void Awake()
    {
        if (!grid)
        {
            var go = new GameObject("RoomsRoot", typeof(Grid));
            grid = go.GetComponent<Grid>();
        }

        var gm = GameManager.Instance;
        if (gm.RoomManager.Grid == null)
            gm.RoomManager.Grid = grid;
    }
    // Should I Change This Func Name?
    public void CreateRooms(List<Node> nodes, MapSO so)
    {
        // ★★★ 이전 스테이지 잔재 정리
        if (rooms == null) rooms = new List<Room>();
        else
        {
            // 파괴된 객체 제거
            rooms.RemoveAll(r => r == null);
            // 새로 만들 거니까 깔끔히 비움
            rooms.Clear();
        }

        this.so = so;
        roomDatas = new List<RoomInitData>();

        convertNodesIntoRoom(nodes);
        setRoomspace();
        GenerateRoom();
    }
    private void convertNodesIntoRoom(List<Node> nodes)
    {
        if (so == null) { Debug.LogError("RoomGenerator: MapSO is not assigned!"); return; }
        if (nodes == null || nodes.Count == 0) { Debug.LogError("RoomGenerator: Nodes empty!"); return; }

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) { Debug.LogError($"RoomGenerator: Node {i} is null!"); continue; }

            var space = n.SpaceArea;
            if (space.width <= 0 || space.height <= 0)
            {
                Debug.LogError($"RoomGenerator: Node {i} has invalid SpaceArea!");
                continue;
            }

            var room = new RoomInitData(n);
            if (i == 0) room.RoomType = RoomType.Start;
            else if (i == nodes.Count - 1) room.RoomType = RoomType.Boss;
            else room.RoomType = RoomType.Normal;

            roomDatas.Add(room);
        }
    }
    private void setRoomspace()
    {
        HashSet<RectInt> usedSpaces = new HashSet<RectInt>();
        foreach (var room in roomDatas)
        {
            RectInt space = room.Node.SpaceArea;

            while (usedSpaces.Contains(space))
                space.position += Vector2Int.right;

            room.RoomSpace = space;
            usedSpaces.Add(space);
        }
    }
    private void GenerateRoom()
    {
        if (so == null)
        {
            Debug.LogError("[RoomGenerator] GenerateRoom called without MapSO");
            return;
        }
        if (!grid)
        {
            Debug.LogError("[RoomGenerator] Grid is missing. Check Awake() or assign in Inspector.");
            return;
        }

        Vector2Int minRoomSz = so.MinRoomSize;
        Vector2Int maxRoomSz = so.MaxRoomSize;

        foreach (var room in roomDatas)
        {
            Room viewRoom = null;

            switch (room.RoomType)
            {
                case RoomType.Normal:
                    {
                        // Parent (방 루트)
                        GameObject tileParent = new GameObject($"RoomTilemap_{room.Node.Id}");
                        tileParent.tag = "Room"; // ★ 방 태그 보장
                        tileParent.transform.position = new Vector3(room.RoomSpace.position.x, room.RoomSpace.position.y);
                        tileParent.transform.SetParent(grid.transform);

                        // 실제 타일맵
                        GameObject tilemapObj = new GameObject("Tilemap");
                        var tilemap = tilemapObj.AddComponent<Tilemap>();
                        var tileRigid = tilemapObj.AddComponent<Rigidbody2D>();
                        tilemapObj.AddComponent<TilemapCollider2D>();
                        tileRigid.constraints = RigidbodyConstraints2D.FreezeAll;
                        tilemapObj.AddComponent<TilemapRenderer>();
                        tilemap.tag = "Ground";

                        PlaceWalls(tilemap, room, minRoomSz, maxRoomSz);
                        locateRoom(tilemapObj, room);

                        tilemapObj.transform.SetParent(tileParent.transform, false);
                        tilemapObj.transform.localPosition = Vector3.zero;

                        placePlatforms(tileParent, tilemap, room);
                        addSpawnPointObject(tileParent, tilemap, room);

                        viewRoom = tileParent.AddComponent<Room>();
                        viewRoom.Initialize(room);
                        break;
                    }

                case RoomType.Start:
                    {
                        if (!so.StartRoom)
                        {
                            Debug.LogError("[RoomGenerator] MapSO.StartRoom prefab is null.");
                            break;
                        }

                        GameObject startRoomObj = locateSpecificRoom(room, so.StartRoom);
                        if (!startRoomObj)
                        {
                            Debug.LogError("[RoomGenerator] StartRoom 배치 실패 (Tilemap 없음/크기 불일치).");
                            break;
                        }
                        startRoomObj.tag = "Room"; // ★

                        viewRoom = startRoomObj.AddComponent<Room>();
                        viewRoom.Initialize(room);

                        // SpawnPoint 처리(생략: 기존 코드 그대로)
                        Transform spawnPoint = startRoomObj.transform.Find("SpawnPoint");
                        if (spawnPoint == null)
                        {
                            var tile = startRoomObj.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
                            Vector3 worldCenter = startRoomObj.transform.position;
                            if (tile != null)
                            {
                                tile.CompressBounds();
                                var b = tile.cellBounds;
                                var cellCenter = new Vector3Int(
                                    Mathf.FloorToInt((b.xMin + b.xMax) * 0.5f),
                                    Mathf.FloorToInt((b.yMin + b.yMax) * 0.5f),
                                    0
                                );
                                worldCenter = tile.CellToWorld(cellCenter) + tile.tileAnchor;
                            }
                            var sp = new GameObject("SpawnPoint").transform;
                            sp.SetParent(startRoomObj.transform, false);
                            sp.position = worldCenter + new Vector3(0.5f, 1.0f, 0f);
                            spawnPoint = sp;
                        }

                        if (GameManager.Instance && GameManager.Instance.RoomManager)
                            GameManager.Instance.RoomManager.SetStartPoint(spawnPoint.position);

                        break;
                    }

                case RoomType.Boss:
                    {
                        if (!so.BossRoom)
                        {
                            Debug.LogError("[RoomGenerator] MapSO.BossRoom prefab is null.");
                            break;
                        }

                        GameObject bossRoomObj = locateSpecificRoom(room, so.BossRoom);
                        if (!bossRoomObj)
                        {
                            Debug.LogError("[RoomGenerator] BossRoom 배치 실패 (Tilemap 없음 또는 크기 불일치).");
                            break;
                        }
                        bossRoomObj.tag = "Room"; // ★

                        viewRoom = bossRoomObj.AddComponent<Room>();
                        viewRoom.Initialize(room);
                        break;
                    }
            }

            if (viewRoom != null)
                rooms.Add(viewRoom);
            else
                Debug.LogWarning($"[RoomGenerator] RoomType `{room.RoomType}` 생성 실패 → 리스트 미추가");
        }
    }
    private GameObject locateSpecificRoom(RoomInitData room, GameObject specific)
    {
        GameObject prefabInstance = Instantiate(specific, grid.transform);

        // ★ Ground/Wall 등 여러 타일맵을 합산해 실제 크기/원점 계산
        // 필요 시 필터링 이름을 지정 (없으면 모든 타일맵 합산)
        HashSet<string> names = null;
        // 예: names = new HashSet<string>{ "Ground", "Wall" };  // 프리팹의 타일맵 오브젝트명이 이렇다면 사용

        if (!TilemapBoundsUtil.TryGetCompositeCellBounds(prefabInstance.transform, names, out var composite, out var minCell, out var size))
        {
            // 폴백: 단일 Tilemap만 있는 구형 프리팹
            var tilemap = prefabInstance.GetComponentInChildren<Tilemap>();
            if (tilemap == null) { Destroy(prefabInstance); return null; }

            tilemap.CompressBounds();
            var b = tilemap.cellBounds;
            minCell = b.position;
            size = new Vector2Int(b.size.x, b.size.y);
        }

        int maxX = room.Node.SpaceArea.xMax - size.x;
        int maxY = room.Node.SpaceArea.yMax - size.y;
        int minX = room.Node.SpaceArea.xMin;
        int minY = room.Node.SpaceArea.yMin;

        // 방에 못 들어가는 크기면 왼하단 정렬(-width,-height 보정 개념 포함)
        if (maxX < minX || maxY < minY)
        {
            prefabInstance.transform.position = new Vector2(minX - minCell.x, minY - minCell.y);
            return prefabInstance;
        }

        int randomX = Random.Range(minX, maxX + 1);
        int randomY = Random.Range(minY, maxY + 1);

        // ★ 합성 bounds의 최소셀(minCell)을 빼서 "왼하단 원점 정렬"
        prefabInstance.transform.position = new Vector2(randomX - minCell.x, randomY - minCell.y);

        room.RoomSpace = new RectInt(randomX, randomY, size.x, size.y);
        return prefabInstance;
    }

    #region DRAW_NORMAL_ROOM
    private void PlaceWalls(Tilemap tile, RoomInitData room, Vector2Int soMinRoomSize, Vector2Int soMaxRoomSize)
    {
        RectInt spaceArea = room.Node.SpaceArea;

        int width = Random.Range(soMinRoomSize.x, Mathf.Max(soMaxRoomSize.x + 1, spaceArea.width + 1));
        int height = Random.Range(soMinRoomSize.y, Mathf.Max(soMaxRoomSize.y + 1, spaceArea.height + 1));

        room.RoomSpace.width = width;
        room.RoomSpace.height = height;

        // 상하
        for (int x = 0; x < width; x++)
        {
            var g = GetRandomTile(so.Ground);
            var c = GetRandomTile(so.Ceiling);
            if (g) tile.SetTile(new Vector3Int(x, 0, 0), g);
            if (c) tile.SetTile(new Vector3Int(x, height, 0), c);
        }

        // 좌우
        for (int y = 0; y < height; y++)
        {
            var l = GetRandomTile(so.LeftWall);
            var r = GetRandomTile(so.RightWall);
            if (l) tile.SetTile(new Vector3Int(0, y, 0), l);
            if (r) tile.SetTile(new Vector3Int(width, y, 0), r);
        }

        // 코너
        var bl = GetRandomTile(so.BottomLeftWall);
        var br = GetRandomTile(so.BottomRightWall);
        var tl = GetRandomTile(so.TopLeftWall);
        var tr = GetRandomTile(so.TopRightWall);
        if (bl) tile.SetTile(new Vector3Int(0, 0, 0), bl);
        if (br) tile.SetTile(new Vector3Int(width, 0, 0), br);
        if (tl) tile.SetTile(new Vector3Int(0, height, 0), tl);
        if (tr) tile.SetTile(new Vector3Int(width, height, 0), tr);
    }

    private void locateRoom(GameObject tilemapObj, RoomInitData room)
    {
        // 부모(tileParent)는 worldPos = room.RoomSpace.position
        // 여기선 local 오프셋만 조절
        RectInt space = room.RoomSpace;

        int width = space.width;
        int height = space.height;

        int offsetX = Mathf.Clamp(Random.Range(0, space.width - width + 1), 0, int.MaxValue);
        int offsetY = Mathf.Clamp(Random.Range(0, space.height - height + 1), 0, int.MaxValue);

        tilemapObj.transform.SetParent(tilemapObj.transform.parent, false);
        tilemapObj.transform.localPosition = new Vector3(offsetX, offsetY, 0);
    }

    private void placePlatforms(GameObject tileObj, Tilemap parentTile, RoomInitData room)
    {
        var platformGO = new GameObject("PlatformTilemap");
        platformGO.transform.SetParent(tileObj.transform, false);
        var platformTM = platformGO.AddComponent<Tilemap>();
        platformGO.AddComponent<TilemapRenderer>();

        var tileCol = platformGO.AddComponent<TilemapCollider2D>();
        tileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comCol = platformGO.AddComponent<CompositeCollider2D>();
        comCol.usedByEffector = true;
        var eff = platformGO.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.useOneWayGrouping = true;
        eff.surfaceArc = 180f;

        parentTile.CompressBounds();
        var b = parentTile.cellBounds;
        int minX = b.xMin, maxX = b.xMax, width = maxX - minX;
        int floorY = b.yMin + 1, ceilingY = b.yMax - 1;

        int platformWidth = Mathf.Max(1, width - 4);
        int startX = minX + (width - platformWidth) / 2;

        var player = Object.FindFirstObjectByType<PlayerMovement>();
        float stepY;
        if (player != null)
        {
            float v0 = player.JumpForce / player.Mass;
            float g = Mathf.Abs(Physics2D.gravity.y);
            float maxJumpH = (v0 * v0) / (2f * g);
            stepY = Mathf.Max(2f, maxJumpH * 0.8f);
        }
        else
        {
            Debug.LogWarning("PlayerMovement 미발견, 기본 점프 간격 2 사용");
            stepY = 2f;
        }

        float totalH = ceilingY - floorY;
        int count = Mathf.CeilToInt(totalH / stepY);
        for (int i = 1; i <= count; i++)
        {
            float yLocal = floorY + stepY * i;
            if (yLocal >= ceilingY) break;
            int yCell = Mathf.FloorToInt(yLocal);

            if (so.MiddlePlatforms != null && so.MiddlePlatforms.Length > 0)
            {
                var tile = so.MiddlePlatforms[Random.Range(0, so.MiddlePlatforms.Length)];
                for (int x = startX; x < startX + platformWidth; x++)
                    platformTM.SetTile(new Vector3Int(x, yCell, 0), tile);
            }
        }

        var rigid = platformGO.GetComponent<Rigidbody2D>();
        if (!rigid) rigid = platformGO.AddComponent<Rigidbody2D>();
        rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        platformGO.tag = "Platform";
        platformGO.layer = 8;
    }
    #endregion

    #region SET_SPAWNPOINT_NORMAL_ROOM
    private void addSpawnPointObject(GameObject tileParent, Tilemap tilemap, RoomInitData room)
    {
        var setup = tileParent.AddComponent<SpawnerController>();

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;

        int spawnCount = Random.Range(2, 5);
        var cells = new List<Vector3Int>();
        for (int i = 0; i < spawnCount; i++)
        {
            int x = Random.Range(bounds.xMin + 1, bounds.xMax - 1);
            int y = bounds.yMin + 1; // 바닥 바로 위
            cells.Add(new Vector3Int(x, y, 0));
        }

        setup.Initialize(tilemap, cells);
    }

    private Tile GetRandomTile(Tile[] tileArray)
    {
        if (tileArray == null || tileArray.Length == 0)
        {
            Debug.LogWarning("[RoomGenerator] 타일 배열이 비어있습니다.");
            return null;
        }
        return tileArray[Random.Range(0, tileArray.Length)];
    }
    #endregion

    private Transform FindRoomsRootInActiveScene()
    {
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!active.IsValid()) return null;
        foreach (var go in active.GetRootGameObjects())
        {
            if (go.name.Equals("RoomsRoot", System.StringComparison.OrdinalIgnoreCase))
                return go.transform;
        }
        return null;
    }
}
