using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
        // Grid 자동 탐색 (상위 → 씬 전체)
        if (!grid) grid = GetComponentInParent<Grid>();
#if UNITY_6000_0_OR_NEWER
        if (!grid) grid = FindFirstObjectByType<Grid>();
#else
        if (!grid) grid = FindObjectOfType<Grid>();
#endif
        // 최후 폴백: 만들기
        if (!grid)
        {
            var go = new GameObject("Grid", typeof(Grid));
            grid = go.GetComponent<Grid>();
        }
    }

    // Should I Change This Func Name?
    public void CreateRooms(List<Node> nodes, MapSO so)
    {
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

            // 같은 위치가 겹치면 우측으로 한 칸씩 밀어 중복 회피
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

                        viewRoom = startRoomObj.AddComponent<Room>();
                        viewRoom.Initialize(room);

                        // ① 우선 프리팹에 'SpawnPoint'가 있으면 사용
                        // ...case RoomType.Start: 안쪽
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
                        // (중략: 없으면 중앙으로 폴백 생성하는 로직)
                        if (GameManager.Instance && GameManager.Instance.RoomManager)
                        {
                            GameManager.Instance.RoomManager.SetStartPoint(spawnPoint.position);
                            Debug.Log($"[RoomGenerator] StartPoint pushed to RoomManager: {spawnPoint.position}");
                        }
                        else
                        {
                            Debug.LogError("[RoomGenerator] RoomManager not found when trying to SetStartPoint.");
                        }

                        // ② 없으면 방 중앙을 계산해 임시 SpawnPoint 생성 (폴백)
                        if (spawnPoint == null)
                        {
                            var tile = startRoomObj.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
                            Vector3 worldCenter;
                            if (tile != null)
                            {
                                tile.CompressBounds();
                                var b = tile.cellBounds;
                                var cellCenter = new Vector3Int(
                                    Mathf.FloorToInt((b.xMin + b.xMax) * 0.5f),
                                    Mathf.FloorToInt((b.yMin + b.yMax) * 0.5f),
                                    0
                                );
                                worldCenter = tile.CellToWorld(cellCenter) + tile.tileAnchor; // 대략 중앙
                            }
                            else
                            {
                                // 타일맵이 없을 일은 드물지만, 혹시 몰라서 오브젝트 위치를 폴백
                                worldCenter = startRoomObj.transform.position;
                            }

                            var sp = new GameObject("SpawnPoint").transform;
                            sp.SetParent(startRoomObj.transform, false);
                            sp.position = worldCenter + new Vector3(0.5f, 1.0f, 0f); // 바닥 관통 방지 약간 올림
                            spawnPoint = sp;
                            Debug.LogWarning("[RoomGenerator] StartRoom에 SpawnPoint가 없어 방 중앙으로 대체 SpawnPoint 생성.");
                        }

                        // ③ GameManager.RoomManager에 시작점 전달
                        if (GameManager.Instance && GameManager.Instance.RoomManager)
                            GameManager.Instance.RoomManager.SetStartPoint(spawnPoint.position);
                        else
                            Debug.LogWarning("[RoomGenerator] GameManager.RoomManager 미발견, 시작 위치 전달 실패");

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

        Tilemap tilemap = prefabInstance.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            // 프리팹에 타일맵이 없다면 배치 불가 → null 반환
            Destroy(prefabInstance);
            return null;
        }

        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        Vector2Int size = new Vector2Int(bounds.size.x, bounds.size.y);
        Vector3Int tileOrigin = bounds.position;

        int maxX = room.RoomSpace.xMax - size.x;
        int maxY = room.RoomSpace.yMax - size.y;
        int minX = room.RoomSpace.xMin;
        int minY = room.RoomSpace.yMin;

        if (maxX < minX || maxY < minY)
        {
            Debug.LogWarning("[RoomGenerator] 특수방 크기가 공간보다 큼 → 시작점에 고정 배치");
            prefabInstance.transform.position = new Vector2(minX - tileOrigin.x, minY - tileOrigin.y);
            return prefabInstance; // ← null 반환 시 이후 분기에서 또 실패하므로, 인스턴스는 유지
        }

        int randomX = Random.Range(minX, maxX + 1);
        int randomY = Random.Range(minY, maxY + 1);

        prefabInstance.transform.position = new Vector2(randomX - tileOrigin.x, randomY - tileOrigin.y);
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

        // 현재 구현은 방 크기 == 공간 크기라 오프셋은 0이 될 가능성이 높음
        int offsetX = Mathf.Clamp(Random.Range(0, space.width - width + 1), 0, int.MaxValue);
        int offsetY = Mathf.Clamp(Random.Range(0, space.height - height + 1), 0, int.MaxValue);

        tilemapObj.transform.SetParent(tilemapObj.transform.parent, false);
        tilemapObj.transform.localPosition = new Vector3(offsetX, offsetY, 0);
    }

    private void placePlatforms(GameObject tileObj, Tilemap parentTile, RoomInitData room)
    {
        // 1) 플랫폼 전용 타일맵
        var platformGO = new GameObject("PlatformTilemap");
        platformGO.transform.SetParent(tileObj.transform, false);
        var platformTM = platformGO.AddComponent<Tilemap>();
        platformGO.AddComponent<TilemapRenderer>();

        // 2) 일방향 콜라이더
        var tileCol = platformGO.AddComponent<TilemapCollider2D>();
        tileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comCol = platformGO.AddComponent<CompositeCollider2D>();
        comCol.usedByEffector = true;
        var eff = platformGO.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.useOneWayGrouping = true;
        eff.surfaceArc = 180f;

        // 3) 기준 경계
        parentTile.CompressBounds();
        var b = parentTile.cellBounds;
        int minX = b.xMin, maxX = b.xMax, width = maxX - minX;
        int floorY = b.yMin + 1, ceilingY = b.yMax - 1;

        // 4) 플랫폼 폭/시작점
        int platformWidth = Mathf.Max(1, width - 4);
        int startX = minX + (width - platformWidth) / 2;

        // 5) 세로 간격
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        float stepY;
        if (player != null)
        {
            float v0 = player.JumpForce / player.Mass;
            float g = Mathf.Abs(Physics2D.gravity.y);
            float maxJumpH = (v0 * v0) / (2f * g);
            stepY = Mathf.Max(2f, maxJumpH * 0.8f); // 너무 촘촘하면 2칸로 바닥
        }
        else
        {
            Debug.LogWarning("PlayerMovement 미발견, 기본 점프 간격 2 사용");
            stepY = 2f;
        }

        // 6) 줄 단위로 플랫폼 생성
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

        // 타일맵 경계
        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;

        // 셀 단위로 2~4개 위치 랜덤 선택
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

    /// <summary>타일 팔레트에서 랜덤 타일</summary>
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
}
