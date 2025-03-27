using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;


public enum portalDir
{
    up,
    down,
    left,
    right
}

public class portalInfo
{
    public portalDir dir;
    // 'id' means Connected Room's Id
    public int id;

    public portalInfo(portalDir dir, int id)
    {
        this.dir = dir;
        this.id = id;
    }
}

public class Node
{
    // Modify Protection Level if we need
    public RectInt SpaceArea;

    // For Defending Dupicated Connections
    public int Id;

    public List<portalInfo> Portals;

    public Node()
    {
        Portals = new List<portalInfo>();
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

    List<Node> leaves;

    List<Node> result;

    #region ROOM_PREFABS
    [field: Header("#Room Prefabs")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject bossRoom;
    [SerializeField] private GameObject shopRoom;
    // TODO - Add Puzzle Maps
    //[SerializeField] private GameObject[] puzzleRooms;
    //[SerializeField] private GameObject midbossRoom;
    #endregion  

    private void Start()
    {
        // 0, 0 : Position of Top-Left corner
        leaves = bsp.GetLeavesByBSP(mapSO);

        // A practically processed map
        Dictionary<Node, List<Node>> adjacent = getAdjacentLeaf(leaves);
        setId(leaves);

        result = mst.GetMSTPath(adjacent);
    }

    private void setId(List<Node> leaves)
    {
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].Id = i;
        }
    }

    // TODO - Change Return Type To Couple(Node, Node)
    private Dictionary<Node, List<Node>> getAdjacentLeaf(List<Node> leaves)
    {
        Dictionary<Node, List<Node>> adjacent = new Dictionary<Node, List<Node>>();

        for (int i = 0; i < leaves.Count; i++)
        {
            List<Node> values = new List<Node>();
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

    private bool isAdjacent(Node a, Node b)
    {
        //return (((a.spaceArea.x == (b.spaceArea.x + b.spaceArea.width)) && (a.spaceArea.y == b.spaceArea.y))
        //    || (((a.spaceArea.x + a.spaceArea.width) == b.spaceArea.x) && (a.spaceArea.y == b.spaceArea.y))
        //    || ((a.spaceArea.y == (b.spaceArea.y + b.spaceArea.height)) && (a.spaceArea.x == b.spaceArea.y))
        //    || (((a.spaceArea.y + a.spaceArea.height) == b.spaceArea.y)) && (a.spaceAre  a.x == b.spaceArea.y));

        bool xAxis = (a.SpaceArea.yMax == b.SpaceArea.yMin || a.SpaceArea.yMin == b.SpaceArea.yMax) && (a.SpaceArea.xMin < b.SpaceArea.xMax && a.SpaceArea  .xMax > b.SpaceArea.xMin);
        bool yAxis = (a.SpaceArea.xMax == b.SpaceArea.xMin || a.SpaceArea.xMin == b.SpaceArea.xMax) && (a.SpaceArea.yMin < b.SpaceArea.yMax && a.SpaceArea.yMax > b.SpaceArea.yMin);

        return xAxis || yAxis;
    }

}
