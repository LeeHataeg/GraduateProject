using System.Collections.Generic;
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

    List<RoomData> roomDatas;

    // Should I Change This Func Name?
    public void CreateRooms(List<MapNode> nodes, MapSO so)
    {
        this.so = so;
        roomDatas = new List<RoomData>();

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
            

            RoomData room = new RoomData(nodes[i]);
            if (i == 0) 
                room.RoomType = RoomType.Start;
            else if(i == nodes.Count - 1) 
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


    // TODO - if(SpaceArea < this.RoomData.RoomSpace)???
    // The room size specified during the special room creation process
    //      may be larger than the space area of ​​the automatically generated room.
    // Solved -> So I'll use the Last Room as an Enterance To the Boss Room
    //      ex) Dungreeed or Skull Boos Room Gate
    //      Seperate Scene
    //private float findMin(MapNode node, Vector2Int mapSz, float min, int index)
    //{
    //    // Check where the current room is located based on the center point of the map.
    //    float initX, initY;
    //    Vector2 initPos = node.SpaceArea.position;
    //    bool left = initPos.x < (mapSz.x / 2);
    //    bool bottom = initPos.y < (mapSz.y / 2);

    //    RoomData roomData = new RoomData(node);

    //    initX = left ? initPos.x : (mapSz.x - initPos.x);
    //    initY = bottom ? initPos.y : (mapSz.y - initPos.y);

    //    roomDatas.Add(roomData);

    //    if (index == 0)
    //    {
    //        return Mathf.Min(initX, initY);
    //    }
    //    else
    //    {
    //        float thisMin = Mathf.Min(initX, initY);
    //        return (thisMin < min) ? thisMin : min;
    //    }
    //}

    //

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
        Vector2Int minRoomSz = so.MinRoomSize; 
        Vector2Int maxRoomSz = so.MaxRoomSize;
        foreach (var room in roomDatas)
        {
            switch (room.RoomType)
            {
                case RoomType.Normal:
                    GameObject tileParent = new GameObject($"RoomTilemap_{room.Node.Id}");
                    tileParent.transform.position = new Vector3(room.RoomSpace.position.x, room.RoomSpace.position.y);
                    tileParent.transform.parent = grid.transform;

                    GameObject tilemapObj = new GameObject("Tilemap");
                    Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
                    tilemapObj.AddComponent<TilemapRenderer>();

                    PlaceWalls(tilemap, room, minRoomSz, maxRoomSz);
                    locateRoom(tilemapObj, room);

                    tilemapObj.transform.parent = tileParent.transform;
                    tilemapObj.transform.localPosition = Vector2.zero;

                    placePlatforms(tilemap, room);
                    addSpawnPointObject(tileParent, room);
                    break;
                case RoomType.Start:
                    // TODO : Need Null Check
                    GameObject startRoomObj = locateSpecificRoom(room, so.StartRoom);
                    addSpawnPointObject(startRoomObj, room);
                    break;
                case RoomType.Shop:
                    // TODO : 9 String
                    break;
                case RoomType.Boss:
                    // TODO : Need Null Check
                    GameObject bossRoomObj = locateSpecificRoom(room, so.BossRoom);
                    addSpawnPointObject(bossRoomObj, room);
                    break;
                case RoomType.SemiBoss:
                    // TODO : 9 String
                    break;
            }
        }
    }

    private GameObject locateSpecificRoom(RoomData room, GameObject specific)
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

        Debug.Log($"specialroom : {specific.name}, pos : {specific.transform.position} , size : {bounds.size}");
        Debug.Log($"target: {room.RoomSpace.position}, size: {room.RoomSpace.size}");

        return prefabInstance;
    }

    #region DRAW_NORMAL_ROOM
    private void PlaceWalls(Tilemap tile, RoomData room, Vector2Int soMinRoomSize, Vector2Int soMaxRoomSize)
    {
        RectInt spaceArea = room.Node.SpaceArea;
        int width = Random.Range(so.MinRoomSize.x, ((spaceArea.width + 1) > (soMaxRoomSize.x + 1)? (spaceArea.width + 1): (soMaxRoomSize.x + 1)));
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

    private void locateRoom(GameObject go, RoomData room)
    {
        int width = room.RoomSpace.width;
        int height = room.RoomSpace.height;
        RectInt spaceArea = room.Node.SpaceArea;

        int posX = Random.Range(spaceArea.x, spaceArea.xMax - width + 1);
        int posY = Random.Range(spaceArea.x, spaceArea.xMax - width + 1);

        go.transform.position = new Vector3(posX, posY, 0);
    }

    //TODO
    private void placePlatforms(Tilemap tile, RoomData room)
    {
        foreach (var portal in room.Node.Portals)
        {
            if (portal.dir == portalDir.up)
            {
                Vector2Int platformPosition = new Vector2Int(
                    room.Node.SpaceArea.x + room.Node.SpaceArea.width / 2,
                    room.Node.SpaceArea.y + room.Node.SpaceArea.height - 1
                );

                tile.SetTile((Vector3Int)platformPosition, so.MiddlePlatforms[Random.Range(0, so.MiddlePlatforms.Length)]);
            }
        }
    }

   #endregion

    #region SET_SPAWNPOINT_NORMAL_ROOM
    private void addSpawnPointObject(GameObject tileParent, RoomData room)
    {
        RoomSetup setup = tileParent.AddComponent<RoomSetup>();

        // Spawn Point 부모 생성
        Transform enemyParent = new GameObject("EnemySpawnPoints").transform;
        enemyParent.SetParent(tileParent.transform);
        setup.enemySpawnPoints = enemyParent;

        Transform itemParent = new GameObject("ItemSpawnPoints").transform;
        itemParent.SetParent(tileParent.transform);
        setup.itemSpawnPoints = itemParent;

        CreateDummyPoints(enemyParent, room.Node.SpaceArea);
        CreateDummyPoints(itemParent, room.Node.SpaceArea);

        setup.Setup(room);
    }
    private void CreateDummyPoints(Transform parent, RectInt area)
    {
        // 방 중심 근처의 임의 위치 몇 군데 생성
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject("SpawnPoint_" + i);
            point.transform.parent = parent;

            float x = Random.Range(area.xMin + 1, area.xMax - 1);
            float y = Random.Range(area.yMin + 1, area.yMax - 1);
            point.transform.localPosition = new Vector3(x - area.xMin, y - area.yMin, 0);
        }
    }
    private Tile GetRandomTile(Tile[] tileArray)
    {
        return tileArray.Length > 0 ? tileArray[Random.Range(0, tileArray.Length)] : null;
    }

    #endregion

}