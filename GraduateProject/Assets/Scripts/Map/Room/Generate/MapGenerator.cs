using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

// ★ 별칭을 'MapNode'로! (메서드 시그니처와 이름까지 일치시킴)
using MapNode = Define.MapNode;

public class MapGenerator : MonoBehaviour
{
    #region Property
    BSPMapDivider bsp;
    MSTPathConnector mst;

    [SerializeField] private MapSO mapSO;

    private List<MapNode> leaves;
    private RoomGenerator roomGenerator;
    private List<MapNode> result;
    private PortalInitializer portalInit;
    #endregion

    private void Awake()
    {
        bsp = new BSPMapDivider();
        mst = new MSTPathConnector();
        roomGenerator = GetComponent<RoomGenerator>();
        portalInit = new PortalInitializer();
    }

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != "InGameScene")
            yield break;


        float t = 2f;
        while (t > 0f && (GameManager.Instance == null || GameManager.Instance.RoomManager == null))
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameManager.Instance == null || GameManager.Instance.RoomManager == null)
        {
            Debug.LogError("[MapGenerator] GameManager/RoomManager not ready. Aborting.");
            yield break;
        }

        var gm = GameManager.Instance;
        if(gm.currentStage <= gm.stages.Count)
        {
            Debug.Log("[MGen]curState : " + gm.currentStage);
            if (gm.stages[gm.currentStage - 1] != null)
                mapSO = gm.stages[gm.currentStage - 1];
        }

        EnsureRoomsRootAndBindToRoomManager();

        if (mapSO == null) { Debug.LogError("[MapGenerator] mapSO is null."); yield break; }
        if (roomGenerator == null) { Debug.LogError("[MapGenerator] RoomGenerator missing."); yield break; }

        yield return GenerateRoutine(mapSO);
    }

    public void Generate(MapSO so)
    {
        // 스테이지 전환 시에도 RoomsRoot 보장
        EnsureRoomsRootAndBindToRoomManager();
        StartCoroutine(GenerateRoutine(so));
    }

    private IEnumerator GenerateRoutine(MapSO so)
    {
        if (so == null) { Debug.LogError("[MapGenerator] GenerateRoutine: MapSO is null."); yield break; }
        if (roomGenerator == null) { Debug.LogError("[MapGenerator] RoomGenerator missing."); yield break; }

        // 0,0 기준 분할
        leaves = bsp.GetLeavesByBSP(so);

        var adjacent = getAdjacentLeaf(leaves);
        setId(leaves);

        result = mst.GetMSTPath(adjacent);

        roomGenerator.CreateRooms(result, so);

        portalInit.SetPortalPrefabAsync();
        portalInit.Init(roomGenerator.rooms);

        // StartRoom 생성 시 RoomGenerator가 RoomManager.SetStartPoint 호출함
        yield return null;
    }

    private void setId(List<MapNode> leaves)
    {
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].Id = i;
    }

    private Dictionary<MapNode, List<MapNode>> getAdjacentLeaf(List<MapNode> leaves)
    {
        var adjacent = new Dictionary<MapNode, List<MapNode>>();

        for (int i = 0; i < leaves.Count; i++)
        {
            var values = new List<MapNode>();
            for (int j = 0; j < leaves.Count; j++)
            {
                if (i == j) continue;

                if (isAdjacent(leaves[i], leaves[j]))
                    values.Add(leaves[j]);
            }
            adjacent.Add(leaves[i], values);
        }

        return adjacent;
    }

    private bool isAdjacent(MapNode a, MapNode b)
    {
        bool xAxis =
            (a.SpaceArea.yMax == b.SpaceArea.yMin || a.SpaceArea.yMin == b.SpaceArea.yMax) &&
            (a.SpaceArea.xMin < b.SpaceArea.xMax && a.SpaceArea.xMax > b.SpaceArea.xMin);

        bool yAxis =
            (a.SpaceArea.xMax == b.SpaceArea.xMin || a.SpaceArea.xMin == b.SpaceArea.xMax) &&
            (a.SpaceArea.yMin < b.SpaceArea.yMax && a.SpaceArea.yMax > b.SpaceArea.yMin);

        return xAxis || yAxis;
    }

    private void EnsureRoomsRootAndBindToRoomManager()
    {
        var active = SceneManager.GetActiveScene();
        if (!active.IsValid()) return;

        GameObject roomsRoot = null;
        foreach (var go in active.GetRootGameObjects())
        {
            if (go.name.Equals("RoomsRoot", System.StringComparison.OrdinalIgnoreCase))
            {
                roomsRoot = go;
                break;
            }
        }
        if (roomsRoot == null)
        {
            var rootGo = new GameObject("RoomsRoot", typeof(Grid));
            roomsRoot = rootGo;
            SceneManager.MoveGameObjectToScene(rootGo, active);
        }

        // RoomManager에 roomsRoot 연결
        var rm = GameManager.Instance?.RoomManager;
        if (rm != null)
            rm.Grid = roomsRoot.GetComponent<Grid>();
    }
}
