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
            // TODO : Room Type 지정 시발 오줌마려
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
            Debug.Log("ㅅㄱ");
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

                    viewRoom = tileParent.AddComponent<Room>();
                    viewRoom.Initialize(room);
                    placePlatforms(tilemap, room);
                    addSpawnPointObject(tileParent, room);
                    break;
                case RoomType.Start:
                    GameObject startRoomObj = locateSpecificRoom(room, so.StartRoom);

                    viewRoom = startRoomObj.AddComponent<Room>();
                    viewRoom.Initialize(room);

                    addSpawnPointObject(startRoomObj, room);
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

                    addSpawnPointObject(bossRoomObj, room);
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

    private void placePlatforms(Tilemap tilemap, RoomInitData room)
    {
        tilemap.CompressBounds();
        BoundsInt b = tilemap.cellBounds;
        int centerX = (b.xMin + b.xMax - 1) / 2;  // 가운데 열
        int floorY = b.yMin + 1;                // 바닥 바로 위
        int ceilingY = b.yMax - 1;                // 천장 바로 아래

        // 2) 플레이어 점프 최대 높이 계산
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        float stepY;
        if (player != null)
        {
            float v0 = player.JumpForce / player.Mass;          // 초기 속도
            float g = Mathf.Abs(Physics2D.gravity.y);         // 중력가속도
            float maxJumpH = (v0 * v0) / (2f * g);              // 수직 최대 높이
            stepY = maxJumpH * 0.9f;                            // 10% 여유 두기
        }
        else
        {
            Debug.LogWarning("PlayerMovement를 찾을 수 없어 기본 거리 2로 설정합니다.");
            stepY = 2f;
        }

        // 3) 방 높이만큼 몇 번 점프가 필요한지 계산
        float totalH = ceilingY - floorY;
        int count = Mathf.CeilToInt(totalH / stepY);

        // 4) 각 스텝마다 플랫폼 타일 설치
        for (int i = 1; i <= count; i++)
        {
            float yLocal = floorY + stepY * i;
            if (yLocal >= ceilingY) break;

            int yCell = Mathf.FloorToInt(yLocal);
            var tile = so.MiddlePlatforms[
                Random.Range(0, so.MiddlePlatforms.Length)
            ];
            tilemap.SetTile(new Vector3Int(centerX, yCell, 0), tile);
        }

        // 5) 플랫폼용 컨트롤러 붙이기 (한 번만)
        var go = tilemap.gameObject;
        if (go.GetComponent<PlatformController>() == null)
            go.AddComponent<PlatformController>();
    }

    #endregion

    #region SET_SPAWNPOINT_NORMAL_ROOM
    private void addSpawnPointObject(GameObject tileParent, RoomInitData room)
    {
        SpawnerController setup = tileParent.AddComponent<SpawnerController>();

        // Spawn Point 부모 생성
        Transform enemyParent = new GameObject("EnemySpawnPoints").transform;
        enemyParent.SetParent(tileParent.transform);
        //setup.enemySpawnPoints = enemyParent;

        Transform itemParent = new GameObject("ItemSpawnPoints").transform;
        itemParent.SetParent(tileParent.transform);
        //setup.itemSpawnPoints = itemParent;

        CreateDummyPoints(enemyParent, room.Node.SpaceArea);
        CreateDummyPoints(itemParent, room.Node.SpaceArea);

        //setup.Setup(room);
    }

    private void CreateDummyPoints(Transform parent, RectInt area)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject("SpawnPoint_" + i);
            point.transform.parent = parent;

            float x = Random.Range(area.xMin + 1, area.xMax - 1);
            float y = Random.Range(area.yMin + 1, area.yMax - 1);
            point.transform.localPosition = new Vector3(x - area.xMin, y - area.yMin, 0);
        }
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