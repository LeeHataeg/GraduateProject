using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using MapNode = Define.MapNode;

public class MapGenerator : MonoBehaviour
{
    BSPMapDivider bsp;
    MSTPathConnector mst;

    [SerializeField] private MapSO mapSO;

    private List<MapNode> leaves;
    private RoomGenerator roomGenerator;
    private List<MapNode> result;
    private PortalInitializer portalInit;

    void Awake()
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

        // GM/RoomManager 준비 기다리기
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
        if (gm.CurrentStage <= gm.Stages.Count && gm.Stages[gm.CurrentStage - 1] != null)
            mapSO = gm.Stages[gm.CurrentStage - 1];

        // ★ RoomsRoot는 항상 씬 로컬 보장
        gm.RoomManager.EnsureRoomsRootIsSceneLocal();

        if (mapSO == null) { Debug.LogError("[MapGenerator] mapSO is null."); yield break; }
        if (roomGenerator == null) { Debug.LogError("[MapGenerator] RoomGenerator missing."); yield break; }

        yield return GenerateRoutine(mapSO);
    }

    public void Generate(MapSO so)
    {
        // 스테이지 전환 시에도 RoomsRoot를 먼저 보정
        GameManager.Instance?.RoomManager?.EnsureRoomsRootIsSceneLocal();
        StartCoroutine(GenerateRoutine(so));
    }

    private IEnumerator GenerateRoutine(MapSO so)
    {
        if (so == null) { Debug.LogError("[MapGenerator] GenerateRoutine: MapSO is null."); yield break; }
        if (roomGenerator == null) { Debug.LogError("[MapGenerator] RoomGenerator missing."); yield break; }

        // 혹시 이전 방이 남아있다면 한 프레임 쉬고 그리는 편이 안전
        yield return null;

        leaves = bsp.GetLeavesByBSP(so);
        var adjacent = getAdjacentLeaf(leaves);
        setId(leaves);
        result = mst.GetMSTPath(adjacent);

        roomGenerator.CreateRooms(result, so);

        portalInit.SetPortalPrefabAsync();
        portalInit.Init(roomGenerator.rooms);

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
                if (isAdjacent(leaves[i], leaves[j])) values.Add(leaves[j]);
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
}
