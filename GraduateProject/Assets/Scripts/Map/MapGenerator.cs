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
    public RectInt spaceArea;
    private RectInt roomArea;

    // For Defending Dupicated Connections
    public int id;

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
    RoomPlacer room;
    #endregion

    [SerializeField] MapSO mapSO;

    List<Node> leaves;

    Dictionary<int, Node> result;

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

        placeTilemapPrefabs();
    }

    // Set Room's Outline
    private void setRoomsOutline(Node node)
    {
        //Node node;
        //node.id;
        //// TODO : Think about Nesting lines on each adjacent nodes
        //if ((node.leftNode == null) && (node.rightNode == null))
        //{
        //    int roomWidth = Random.Range(minRoomSize.x, Mathf.Min(node.spaceArea.width, maxRoomSize.x));
        //    int roomHeight = Random.Range(minRoomSize.y, Mathf.Min(node.spaceArea.height, maxRoomSize.y));
        //    int roomX = node.spaceArea.x + Random.Range(0, node.spaceArea.width - roomWidth);
        //    int roomY = node.spaceArea.y + Random.Range(0, node.spaceArea.height - roomHeight);
        //    node.roomArea = new RectInt(roomX, roomY, roomWidth, roomHeight);
        //}
        //else
        //{
        //    if (node.leftNode != null)
        //    {
        //        SetRoomsOutline(node.leftNode);
        //    }
        //    if (node.rightNode != null)
        //    {
        //        SetRoomsOutline(node.rightNode);
        //    }
        //}
    }

    private void setId(List<Node> leaves)
    {
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].id = i;
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
        return (((a.spaceArea.x == (b.spaceArea.x + b.spaceArea.width)) && (a.spaceArea.y == b.spaceArea.y))
            || (((a.spaceArea.x + a.spaceArea.width) == b.spaceArea.x) && (a.spaceArea.y == b.spaceArea.y))
            || ((a.spaceArea.y == (b.spaceArea.y + b.spaceArea.height)) && (a.spaceArea.x == b.spaceArea.y))
            || (((a.spaceArea.y + a.spaceArea.height) == b.spaceArea.y)) && (a.spaceArea.x == b.spaceArea.y));
    }

    private void locateSpecialRoom()
    {
        // TODO - Before Typing this script,
        //      Write "Room.cs" scipt

        // TODO - place special rooms(boss, start) at each end of longest route
        //  using BFS
    }

    // this code locates prefabs along the outline
    private void placeTilemapPrefabs()
    {
        //
    }
}
