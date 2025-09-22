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

        // GameManager/RoomManager 보장
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

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

        if (mapSO == null) { Debug.LogError("[MapGenerator] mapSO is null."); yield break; }
        if (roomGenerator == null) { Debug.LogError("[MapGenerator] RoomGenerator missing."); yield break; }

        // 0,0 기준 분할
        leaves = bsp.GetLeavesByBSP(mapSO);

        var adjacent = getAdjacentLeaf(leaves);
        setId(leaves);

        result = mst.GetMSTPath(adjacent);

        roomGenerator.CreateRooms(result, mapSO);

        portalInit.SetPortalPrefabAsync();
        portalInit.Init(roomGenerator.rooms);
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
}
