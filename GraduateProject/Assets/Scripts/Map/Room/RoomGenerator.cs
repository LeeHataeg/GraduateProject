using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Convert SpaceArea into Real Room
public class RoomGenerator : MonoBehaviour
{
    public MapSO So;
    [SerializeField] private Tilemap tilemap;

    List<RoomData> roomDatas;

    Node startRoom;
    Node bossRoom;

    // Should I Change This Func Name?
    public void CreateRooms(List<Node> nodes)
    {
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


    // TODO - this code is a temparary script
    //  This will be replaced by scripts under this code.
    private void convertToStraitLine(List<Node> nodes)  // Temp Code
    {
        int count = 0;
        while (count < 2)
        {
            foreach (Node node in nodes)
            {
                RoomData rd = new RoomData(node);
                if (node.Portals.Count == 1)
                {
                    if (startRoom == null)
                    {
                        startRoom = node;
                        rd.RoomType = RoomType.Start;
                    }
                    else
                    {
                        bossRoom = node;
                        rd.RoomType = RoomType.Boss;
                    }
                    count++;
                }
                else
                {
                    rd.RoomType = RoomType.Normal;
                }
                roomDatas.Add(rd);
            }
        }
    }

    private void convertNodesIntoRoom(List<Node> nodes)
    {
        if (So == null)
        {
            Debug.LogError("RoomGenerator: MapSO (So) is not assigned!");
            return;
        }
        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogError("RoomGenerator: Nodes list is empty!");
            return;
        }

        Vector2Int mapSz = So.MapSize;
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
    private float findMin(Node node, Vector2Int mapSz, float min, int index)
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

    private void findFarRoom()
    {
        // (X) Step 01. Find Outermost Room -> Done

        // Step 02. Find Furthest From It

        // Step 03. Set the Two Rooms as the Starting Room and the Boss Room
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
            tilemap.SetTile(new Vector3Int(x, area.yMin, 0), GetRandomTile(So.Ground));
            tilemap.SetTile(new Vector3Int(x, area.yMax - 1, 0), GetRandomTile(So.Ceiling));
        }

        for (int y = area.yMin; y < area.yMax; y++)
        {
            tilemap.SetTile(new Vector3Int(area.xMin, y, 0), GetRandomTile(So.LeftWall));
            tilemap.SetTile(new Vector3Int(area.xMax - 1, y, 0), GetRandomTile(So.RightWall));
        }

        tilemap.SetTile(new Vector3Int(area.xMin, area.yMax - 1, 0), GetRandomTile(So.TopLeftWall));
        tilemap.SetTile(new Vector3Int(area.xMax - 1, area.yMax - 1, 0), GetRandomTile(So.TopRightWall));
        tilemap.SetTile(new Vector3Int(area.xMin, area.yMin, 0), GetRandomTile(So.BottomLeftWall));
        tilemap.SetTile(new Vector3Int(area.xMax - 1, area.yMin, 0), GetRandomTile(So.BottomRightWall));
    }

    private void GenerateRoom()
    {
        foreach (RoomData room in roomDatas)
        {
            // TODO - it's a temp code
            // SpaceArea.position -> RoomData.RoomSpace.position
            Vector3 pos = new Vector3(room.Node.SpaceArea.position.x, room.Node.SpaceArea.position.y, 0f);

            GameObject roomPrefab = Instantiate(new GameObject(), pos, Quaternion.identity);
            RoomSetup setup = roomPrefab.GetComponent<RoomSetup>();
            setup.Setup(room);
        }
    }
    private Tile GetRandomTile(Tile[] tileArray)
    {
        return tileArray.Length > 0 ? tileArray[Random.Range(0, tileArray.Length)] : null;
    }

    #region TODO
    // 1. 외곽선 제외
    // 2. 스폰될 대상 확인
    // 2-1 몬스터
    // 2-1-1 나는 몹 -> 공중에 배치(해당 포인트 근처에 일정 범위내에 아무것도 배치 안되도록)
    // 2-1-2 뚜벅이 -> 발판과 가깝게 배치
    // 2-1-3 기타
    // 2-2 보상 (상자)
    // 2-2-1 클리어 및 일반 보상 -> 맵 중앙 하단(바닥) 등에 출현하도록
    // 2-2-2 퍼즐 등의 보상 -> 퍼즐 클리어 이후 퍼즐 프리팹의 지정된 위치에 스폰되도록
    // 2-3 배경
    //  : 상호작용 불가능하나 배경과 무관하게 맵에 배치되는 장식들(횃불, 덩굴, 해골 등등)
    // 2-4 포탈과 출입구
    // 2-5 단독 아이템
    //  : 주로 몬스터 처치 보상 시 드랍(Enemy 관련 코드로?)
    //      - 아니다 그냥 SpawnByChar 등으로 작성하고 파라미터로 객체를 넣으면,
    //      해당 객체의 position과 collider 범위 기준으로 그 근방에 떨어뜨리도록
    // 2-6 함정
    // 2-6-1 덫
    // 2-6-2 뾰족뾰족
    // 2-6-3 화살(스컬의 화살 발사기 같은)
    // 2-6-4 낙사 존 (피 까이고 특정 포인트에서 리스폰되도록)
    //      -> 여유 되면 낙사 시 피까이면서 미니맵 상 멀리 아래 존재하는 다른 방으로 떨어뜨리는 것도 재밌을듯
    // 2-7 스폰 포인트 : 맵 이동, 스테이지 진입 시 캐릭터가 등장할 위치
    // 2-8 NPC
    // 2-9 강화 관련 오브젝트

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

                tilemap.SetTile((Vector3Int)platformPosition, So.MiddlePlatforms[Random.Range(0, So.MiddlePlatforms.Length)]);
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
                tilemap.SetTile((Vector3Int)spawnPosition, So.Ground[Random.Range(0, So.Ground.Length)]);
                return;
            }
        }

        if (!isPlatform)
        {
            spawnPosition = new Vector2Int(
                room.Node.SpaceArea.x + room.Node.SpaceArea.width / 2,
                room.Node.SpaceArea.y
            );
            tilemap.SetTile((Vector3Int)spawnPosition, So.Ground[Random.Range(0, So.Ground.Length)]);
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
    }

    void SpawnObjects(Transform parent, GameObject[] objectList, int count)
    {
        List<Transform> availableSpawns = new List<Transform>(parent.GetComponentsInChildren<Transform>());
        availableSpawns.Remove(parent);

        for (int i = 0; i < count && availableSpawns.Count > 0; i++)
        {
            int index = Random.Range(0, availableSpawns.Count);
            Instantiate(objectList[Random.Range(0, objectList.Length)], availableSpawns[index].position, Quaternion.identity);
            availableSpawns.RemoveAt(index);
        }
    }
}