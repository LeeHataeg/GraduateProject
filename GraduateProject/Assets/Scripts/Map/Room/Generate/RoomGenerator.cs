using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

// Convert SpaceArea into Real Room
public class RoomGenerator : MonoBehaviour
{
    MapSO so;
    [SerializeField] private Grid grid;

    List<RoomInitData> roomDatas;

    public List<Room> rooms = new List<Room>();

    // Should I Change This Func Name?
    public void CreateRooms(List<MapNode> nodes, MapSO so)
    {
        this.so = so;
        roomDatas = new List<RoomInitData>();

        convertNodesIntoRoom(nodes);

        setRoomspace();

        GenerateRoom();
    }

    private void convertNodesIntoRoom(List<MapNode> nodes)
    {
        Vector2Int mapSz = so.MapSize;

        if (so == null)
        {
            Debug.LogError("RoomGenerator: MapSO (So) is not assigned!");
            return;
        }
        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogError("RoomGenerator: Nodes list is empty!");
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
            {
                Debug.LogError($"RoomGenerator: Node at index {i} is null!");
                continue;
            }
            if (nodes[i].SpaceArea == null)
            {
                Debug.LogError($"RoomGenerator: Node {i} has null SpaceArea!");
                continue;
            }


            RoomInitData room = new RoomInitData(nodes[i]);
            if (i == 0)
                room.RoomType = RoomType.Start;
            else if (i == nodes.Count - 1)
                room.RoomType = RoomType.Boss;
            else
                room.RoomType = RoomType.Normal;
            roomDatas.Add(room);
        }
        if (nodes[0] == null)
        {
            Debug.Log("노드 할당 오류");
        }
    }


    private void setRoomspace()
    {
        HashSet<RectInt> usedSpaces = new HashSet<RectInt>();
        foreach (var room in roomDatas)
        {
            RectInt space = room.Node.SpaceArea;

            while (usedSpaces.Contains(space))
            {
                space.position += Vector2Int.right * 1;
            }
            room.RoomSpace = space;
            usedSpaces.Add(space);
        }
    }

    private void GenerateRoom()
    {
        GameObject tileParent;
        GameObject tilemapObj;
        Tilemap tilemap;
        Rigidbody2D tileRigid;

        Vector2Int minRoomSz = so.MinRoomSize;
        Vector2Int maxRoomSz = so.MaxRoomSize;

        foreach (var room in roomDatas)
        {
            Room viewRoom = null;
            switch (room.RoomType)
            {
                case RoomType.Normal:
                    tileParent = new GameObject($"RoomTilemap_{room.Node.Id}");
                    tileParent.transform.position = new Vector3(room.RoomSpace.position.x, room.RoomSpace.position.y);
                    tileParent.transform.parent = grid.transform;

                    tilemapObj = new GameObject("Tilemap");
                    tilemap = tilemapObj.AddComponent<Tilemap>();

                    tileRigid = tilemap.AddComponent<Rigidbody2D>();
                    tilemap.AddComponent<TilemapCollider2D>();
                    tileRigid.constraints = RigidbodyConstraints2D.FreezeAll;
                    tilemapObj.AddComponent<TilemapRenderer>();
                    tilemap.tag = "Ground";

                    PlaceWalls(tilemap, room, minRoomSz, maxRoomSz);
                    locateRoom(tilemapObj, room);

                    tilemapObj.transform.parent = tileParent.transform;
                    tilemapObj.transform.localPosition = Vector2.zero;

                    placePlatforms(tileParent, tilemap, room);
                    addSpawnPointObject(tileParent,tilemap , room);

                    viewRoom = tileParent.AddComponent<Room>();
                    viewRoom.Initialize(room);
                    break;
                case RoomType.Start:
                    GameObject startRoomObj = locateSpecificRoom(room, so.StartRoom);

                    viewRoom = startRoomObj.AddComponent<Room>();
                    viewRoom.Initialize(room);

                    //addSpawnPointObject(startRoomObj, room);
                    Transform spawnPoint = startRoomObj.transform.Find("SpawnPoint");
                    if (spawnPoint != null)
                    {
                        GameManager.Instance.RoomManager.SetStartPoint(spawnPoint.position);
                    }
                    break;
                case RoomType.Boss:
                    GameObject bossRoomObj = locateSpecificRoom(room, so.BossRoom);

                    viewRoom = bossRoomObj.AddComponent<Room>();
                    viewRoom.Initialize(room);

                    //addSpawnPointObject(bossRoomObj, room);
                    break;
            }
            if (viewRoom != null)
                rooms.Add(viewRoom);
            else
                Debug.LogWarning($"RoomGenerator: RoomType `{room.RoomType}` 에서 viewRoom 생성 실패, 리스트에 추가되지 않음.");
        }
    }

    private GameObject locateSpecificRoom(RoomInitData room, GameObject specific)
    {
        GameObject prefabInstance = Instantiate(specific, grid.transform);

        Tilemap tilemap = prefabInstance.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
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
            Debug.LogWarning("특수방 크기가 지정된 공간보다 큽니다. 시작점에 배치합니다.");
            prefabInstance.transform.position = new Vector2(minX - tileOrigin.x, minY - tileOrigin.y);
            return null;
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
        int width = Random.Range(so.MinRoomSize.x, ((spaceArea.width + 1) > (soMaxRoomSize.x + 1) ? (spaceArea.width + 1) : (soMaxRoomSize.x + 1)));
        int height = Random.Range(so.MinRoomSize.y, ((spaceArea.height + 1) > (soMaxRoomSize.y + 1) ? (spaceArea.height + 1) : (soMaxRoomSize.y + 1)));

        room.RoomSpace.width = width;
        room.RoomSpace.height = height;

        for (int x = 0; x < width; x++)
        {
            tile.SetTile(new Vector3Int(x, 0, 0), GetRandomTile(so.Ground));
            tile.SetTile(new Vector3Int(x, height, 0), GetRandomTile(so.Ceiling));
        }

        for (int y = 0; y < height; y++)
        {
            tile.SetTile(new Vector3Int(0, y, 0), GetRandomTile(so.LeftWall));
            tile.SetTile(new Vector3Int(width, y, 0), GetRandomTile(so.RightWall));
        }

        tile.SetTile(new Vector3Int(0, 0, 0), GetRandomTile(so.BottomLeftWall));
        tile.SetTile(new Vector3Int(width, 0, 0), GetRandomTile(so.BottomRightWall));
        tile.SetTile(new Vector3Int(0, height, 0), GetRandomTile(so.TopLeftWall));
        tile.SetTile(new Vector3Int(width, height, 0), GetRandomTile(so.TopRightWall));
    }

    private void locateRoom(GameObject tilemapObj, RoomInitData room)
    {
        // 부모인 tileParent는 이미 worldPos = room.RoomSpace.position
        // 따라서 tilemapObj는 localPosition 만 조절
        RectInt space = room.RoomSpace;

        int width = space.width;
        int height = space.height;

        // localOffsetX: 0 부터 (공간 폭 - 방 폭)
        int offsetX = Random.Range(0, space.width - width + 1);
        // localOffsetY: 0 부터 (공간 높이 - 방 높이)
        int offsetY = Random.Range(0, space.height - height + 1);

        tilemapObj.transform.SetParent(tilemapObj.transform.parent, false);
        tilemapObj.transform.localPosition = new Vector3(offsetX, offsetY, 0);
    }

    private void placePlatforms(GameObject tileObj, Tilemap parentTile, RoomInitData room)
    {
        // 1) Create a dedicated child Tilemap for platforms
        var platformGO = new GameObject("PlatformTilemap");
        platformGO.transform.SetParent(tileObj.transform, false);
        var platformTM = platformGO.AddComponent<Tilemap>();
        platformGO.AddComponent<TilemapRenderer>();

        // 2) One-way collider setup
        var tileCol = platformGO.AddComponent<TilemapCollider2D>();
        tileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comCol = platformGO.AddComponent<CompositeCollider2D>();
        comCol.usedByEffector = true;
        var eff = platformGO.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.useOneWayGrouping = true;
        eff.surfaceArc = 180f;

        // 3) Figure out bounds from your walls-&-floor tilemap
        parentTile.CompressBounds();
        var b = parentTile.cellBounds;
        int minX = b.xMin, maxX = b.xMax, width = maxX - minX;
        int floorY = b.yMin + 1, ceilingY = b.yMax - 1;

        // 4) Platform width and start X
        int platformWidth = Mathf.Max(1, width - 4);
        int startX = minX + (width - platformWidth) / 2;

        // 5) Calculate stepY exactly as before
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        float stepY;
        if (player != null)
        {
            float v0 = player.JumpForce / player.Mass;
            float g = Mathf.Abs(Physics2D.gravity.y);
            float maxJumpH = (v0 * v0) / (2f * g);
            stepY = 3f/*maxJumpH * 0.8f*/;
        }
        else
        {
            Debug.LogWarning("PlayerMovement 미발견, 기본 점프 간격 2 사용");
            stepY = 2f;
        }

        // 6) Stamp in each platform row
        float totalH = ceilingY - floorY;
        int count = Mathf.CeilToInt(totalH / stepY);
        for (int i = 1; i <= count; i++)
        {
            float yLocal = floorY + stepY * i;
            if (yLocal >= ceilingY) break;
            int yCell = Mathf.FloorToInt(yLocal);

            var tile = so.MiddlePlatforms[Random.Range(0, so.MiddlePlatforms.Length)];
            for (int x = startX; x < startX + platformWidth; x++)
                platformTM.SetTile(new Vector3Int(x, yCell, 0), tile);
        }

        var rigid = platformGO.GetComponent<Rigidbody2D>();
        rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        platformGO.tag = "Platform";
        platformGO.layer = 8;
    }

    #endregion

    #region SET_SPAWNPOINT_NORMAL_ROOM
    // RoomGenerator.cs 에서
    private void addSpawnPointObject(GameObject tileParent, Tilemap tilemap, RoomInitData room)
    {
        // 스폰 컨트롤러 붙이고
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
            int y = bounds.yMin + 1;                // 바닥 바로 위
            cells.Add(new Vector3Int(x, y, 0));
        }

        // 초기화 시 타일맵과 셀 목록을 넘겨준다
        setup.Initialize(tilemap, cells);
    }


    /// <summary>
    /// 타일 맵을 채울 타일을 타일 팔레트에서 골라옴.
    /// </summary>
    /// <param name="GetRandomTile"></param>
    /// <returns></returns>
    private Tile GetRandomTile(Tile[] tileArray)
    {
        return tileArray.Length > 0 ? tileArray[Random.Range(0, tileArray.Length)] : null;
    }

    #endregion

}