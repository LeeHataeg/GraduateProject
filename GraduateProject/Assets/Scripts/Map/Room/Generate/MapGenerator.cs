using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;


public enum PortalDir
{
    up,
    down,
    left,
    right
}

public class PortalInfo
{
    public PortalDir dir;
    // 'id' means Connected Room's Id
    public MapNode connected;

    public PortalInfo(PortalDir dir, MapNode connected)
    {
        this.dir = dir;
        this.connected = connected;
    }
}

public class MapNode
{
    // Modify Protection Level if we need
    public RectInt SpaceArea;

    // For Defending Dupicated Connections
    public int Id;

    public List<PortalInfo> Portals;

    public MapNode()
    {
        Portals = new List<PortalInfo>();
    }
}

// TODO - This Class is managed by Other Code
// To Create this Objects along level
public class MapGenerator : MonoBehaviour
{
    #region Instance
    BSPMapDivider bsp;
    MSTPathConnector mst;
    #endregion

    [SerializeField] MapSO mapSO;

    List<MapNode> leaves;

    RoomGenerator roomGenerator;

    List<MapNode> result;

    private void Awake()
    {
        bsp = new BSPMapDivider();
        mst = new MSTPathConnector();
        roomGenerator = GetComponent<RoomGenerator>();
    }

    private void Start()
    {
        // 0, 0 : Position of Top-Left corner
        leaves = bsp.GetLeavesByBSP(mapSO);

        // A practically processed map
        Dictionary<MapNode, List<MapNode>> adjacent = getAdjacentLeaf(leaves);
        setId(leaves);

        result = mst.GetMSTPath(adjacent);

        roomGenerator.CreateRooms(result, mapSO);
    }

    private void setId(List<MapNode> leaves)
    {
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].Id = i;
        }
    }

    // TODO - Change Return Type To Couple(Node, Node)
    private Dictionary<MapNode, List<MapNode>> getAdjacentLeaf(List<MapNode> leaves)
    {
        Dictionary<MapNode, List<MapNode>> adjacent = new Dictionary<MapNode, List<MapNode>>();

        for (int i = 0; i < leaves.Count; i++)
        {
            List<MapNode> values = new List<MapNode>();
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
        bool xAxis = (a.SpaceArea.yMax == b.SpaceArea.yMin || a.SpaceArea.yMin == b.SpaceArea.yMax) && (a.SpaceArea.xMin < b.SpaceArea.xMax && a.SpaceArea  .xMax > b.SpaceArea.xMin);
        bool yAxis = (a.SpaceArea.xMax == b.SpaceArea.xMin || a.SpaceArea.xMin == b.SpaceArea.xMax) && (a.SpaceArea.yMin < b.SpaceArea.yMax && a.SpaceArea.yMax > b.SpaceArea.yMin);

        return xAxis || yAxis;
    }

}
