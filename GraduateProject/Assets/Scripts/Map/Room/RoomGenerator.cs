using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Convert SpaceArea into Real Room
public class RoomGenerator : MonoBehaviour
{
    MapSO so;
    [SerializeField] private Tilemap tilemap;

    List<RoomData> roomDatas;

    MapNode startRoom;                
    MapNode bossRoom;

    // Should I Change This Func Name?
    public void CreateRooms(List<MapNode> nodes, MapSO so)
    {
        this.so = so;
        roomDatas = new List<RoomData>();

        // Step 1: Convert Nodes Into RoomData
        convertNodesIntoRoom(nodes);  // 기존 convertToStraitLine() 대신 사용

        // Step 2: Set Special Room Like Start, Boss Room
        placeSpecialRooms();

        // Step 3: Generate Room GameObjects
        GenerateRoom();

        // Step 4: Place Platforms & Spawn Points
        drawMapTiles();
    }

    private void convertNodesIntoRoom(List<MapNode> nodes)
    {
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

        Vector2Int mapSz = so.MapSize;
        float disMin = 0;

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

            disMin = findMin(nodes[i], mapSz, disMin, i);
        }
    }


    // TODO - if(SpaceArea < this.RoomData.RoomSpace)???
    // The room size specified during the special room creation process
    //      may be larger than the space area of ​​the automatically generated room.

    // Solved -> So I'll use the Last Room as an Enterance To the Boss Room
    //      ex) Dungreeed or Skull Boos Room Gate
    //      Seperate Scene
    private float findMin(MapNode node, Vector2Int mapSz, float min, int index)
    {
        float initX, initY;
        Vector2 initPos = node.SpaceArea.position;
        bool left = initPos.x < (mapSz.x / 2);
        bool bottom = initPos.y < (mapSz.y / 2);

        RoomData roomData = new RoomData(node);

        initX = left ? initPos.x : (mapSz.x - initPos.x);
        initY = bottom ? initPos.y : (mapSz.y - initPos.y);

        roomDatas.Add(roomData);

        if (index == 0)
        {
            return Mathf.Min(initX, initY);
        }
        else
        {
            float thisMin = Mathf.Min(initX, initY);
            return (thisMin < min) ? thisMin : min;
        }
    }

    private void placeSpecialRooms()
    {
        // TODO - Select Shop, SemiBoss
    }

    public void drawMapTiles()
    {
        foreach (var room in roomDatas)
        {
            PlaceWalls(room);
            placePlatforms(room);
            placeSpawnPoint(room);
        }
    }

    private void PlaceWalls(RoomData room)
    {
        RectInt area = room.Node.SpaceArea;
        for (int x = area.xMin; x < area.xMax; x++)
        {
            tilemap.SetTile(new Vector3Int(x, area.yMin, 0), GetRandomTile(so.Ground));
            tilemap.SetTile(new Vector3Int(x, area.yMax - 1, 0), GetRandomTile(so.Ceiling));
        }

        for (int y = area.yMin; y < area.yMax; y++)
        {
            tilemap.SetTile(new Vector3Int(area.xMin, y, 0), GetRandomTile(so.LeftWall));
            tilemap.SetTile(new Vector3Int(area.xMax - 1, y, 0), GetRandomTile(so.RightWall));
        }

        tilemap.SetTile(new Vector3Int(area.xMin, area.yMax - 1, 0), GetRandomTile(so.TopLeftWall));
        tilemap.SetTile(new Vector3Int(area.xMax - 1, area.yMax - 1, 0), GetRandomTile(so.TopRightWall));
        tilemap.SetTile(new Vector3Int(area.xMin, area.yMin, 0), GetRandomTile(so.BottomLeftWall));
        tilemap.SetTile(new Vector3Int(area.xMax - 1, area.yMin, 0), GetRandomTile(so.BottomRightWall));
    }

    private void GenerateRoom()
    {
        Debug.Log("room Count : " + roomDatas.Count);
        foreach (RoomData room in roomDatas)
        {
            Debug.Log(room.RoomType);
        }

        foreach (RoomData room in roomDatas)
        {
            Vector3 pos = new Vector3(room.Node.SpaceArea.position.x, room.Node.SpaceArea.position.y, 0f);

            GameObject roomGO = new GameObject("Room");
            roomGO.transform.position = pos;

            RoomSetup setup = roomGO.AddComponent<RoomSetup>();

            // Spawn Point 부모 생성
            Transform enemyParent = new GameObject("EnemySpawnPoints").transform;
            enemyParent.SetParent(roomGO.transform);
            setup.enemySpawnPoints = enemyParent;

            Transform itemParent = new GameObject("ItemSpawnPoints").transform;
            itemParent.SetParent(roomGO.transform);
            setup.itemSpawnPoints = itemParent;

            CreateDummyPoints(enemyParent, room.Node.SpaceArea);
            CreateDummyPoints(itemParent, room.Node.SpaceArea);

            setup.Setup(room);
        }
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

    #region TODO
    // 1. 외곽선 제외

    // Step01. 외곽선 제외
    // Step02. 방 타입 확인
    // Step03. 
    #endregion
    private void placePlatforms(RoomData room)
    {
        foreach (var portal in room.Node.Portals)
        {
            if (portal.dir == portalDir.up)
            {
                Vector2Int platformPosition = new Vector2Int(
                    room.Node.SpaceArea.x + room.Node.SpaceArea.width / 2,
                    room.Node.SpaceArea.y + room.Node.SpaceArea.height - 1
                );

                tilemap.SetTile((Vector3Int)platformPosition, so.MiddlePlatforms[Random.Range(0, so.MiddlePlatforms.Length)]);
            }
        }
    }

    private void placeSpawnPoint(RoomData room)
    {
        Vector2Int spawnPosition;
        bool isPlatform = false;

        foreach (var portal in room.Node.Portals)
        {
            if (portal.dir == portalDir.up)
            {
                isPlatform = true;
                spawnPosition = new Vector2Int(
                    room.Node.SpaceArea.x + room.Node.SpaceArea.width / 2,
                    room.Node.SpaceArea.y + room.Node.SpaceArea.height - 1
                );
                tilemap.SetTile((Vector3Int)spawnPosition, so.Ground[Random.Range(0, so.Ground.Length)]);
                return;
            }
        }

        if (!isPlatform)
        {
            spawnPosition = new Vector2Int(
                room.Node.SpaceArea.x + room.Node.SpaceArea.width / 2,
                room.Node.SpaceArea.y
            );
            tilemap.SetTile((Vector3Int)spawnPosition, so.Ground[Random.Range(0, so.Ground.Length)]);
        }
    }
}

public class RoomSetup : MonoBehaviour
{
    public Transform enemySpawnPoints;
    public Transform itemSpawnPoints;

    public GameObject[] enemies;
    public GameObject[] items;

    public void Setup(RoomData room)
    {
        // 몬스터 스폰 기능은 아직 구현되지 않았으므로 주석 처리
        /*
        switch (room.RoomType)
        {
            case RoomType.Start:
                SpawnObjects(itemSpawnPoints, items, 1);
                break;
            case RoomType.Boss:
                SpawnObjects(enemySpawnPoints, new GameObject[] { enemies[0] }, 1);
                break;
            case RoomType.Normal:
                SpawnObjects(enemySpawnPoints, enemies, Random.Range(2, 5));
                break;
        }
        */
    }

    // 몬스터 및 아이템 스폰 함수도 임시로 막아둠
    /*
    void SpawnObjects(Transform parent, GameObject[] objectList, int count)
    {
        if (objectList == null || objectList.Length == 0)
        {
            Debug.LogWarning("RoomSetup: objectList가 비어 있어서 아무것도 스폰되지 않음");
            return;
        }

        List<Transform> availableSpawns = new List<Transform>(parent.GetComponentsInChildren<Transform>());
        availableSpawns.Remove(parent);

        for (int i = 0; i < count && availableSpawns.Count > 0; i++)
        {
            int index = Random.Range(0, availableSpawns.Count);
            Instantiate(objectList[Random.Range(0, objectList.Length)], availableSpawns[index].position, Quaternion.identity);
            availableSpawns.RemoveAt(index);
        }
    }
    */
}
