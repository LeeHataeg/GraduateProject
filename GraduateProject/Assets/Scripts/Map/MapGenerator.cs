using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Node
{
    public Node parNode;

    // Sibling Node
    public Node leftNode;
    public Node rightNode;

    public bool dividedHorizontally;

    // Level Design for balence
    public int depth;

    // Tips!
    //      RectInt : Rectangle created by scripts
    //      dividen Space Info : width, height, etc
    public RectInt spaceArea;

    //TODO - Move this valiable
    public RectInt roomArea;

    // 
    public List<Node> connect = new List<Node>();

    public Node(RectInt rect)
    {
        this.spaceArea = rect;
    }

}

// TODO - Move this type into other scipts about room
public enum RoomTypes
{
    StartRoom,
    BossRoom,
    ShopRoom,
    PuzzleRoom,
    MidbossRoom
}

public class MapGenerator : MonoBehaviour
{
    #region MAP_SPRITES
    [field: Header("#Room Components Prefabs")]
    // corner
    [SerializeField] GameObject[] topLeftWall;
    [SerializeField] GameObject[] topRightWall;
    [SerializeField] GameObject[] bottomLeftWall;
    [SerializeField] GameObject[] bottomRightWall;
    // side
    [SerializeField] GameObject[] LeftWall;
    [SerializeField] GameObject[] RightWall;
    // ceiling and ground
    [SerializeField] GameObject[] ground;
    [SerializeField] GameObject[] ceiling;
    #endregion

    #region MAP_VARIABLES
    [field: Header("#Map Variables")]
    [SerializeField] private Vector2Int mapSize;    //Total Size

    [SerializeField] private Vector2Int minSpaceSize;
    [SerializeField] private Vector2Int maxSpaceSize;

    [SerializeField] private Vector2Int minRoomSize;
    [SerializeField] private Vector2Int maxRoomSize;

    //TODO - Two Type of Rate (Wide or Tall)
    [SerializeField] private float maxDevideRate;
    [SerializeField] private float minDevideRate;

    [SerializeField] private int maxDepth;
    #endregion

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
        Node root = new Node(new RectInt(0, 0, mapSize.x, mapSize.y));
        root.depth = 0;

        // A practically processed map
        List<Node> leaves = new List<Node>();
        Dictionary<Node, List<Node>> adjacent = new Dictionary<Node, List<Node>>();

        DivideMap(root);
        SetRoomsOutline(root);
        leaves = GetLeafNodes(root);
        adjacent = GetAdjacentLeaf(leaves);
        ConnectRooms(adjacent);
        PlaceTilemapPrefabs();
    }

    // This code divides Space
    private void DivideMap(Node node)
    {
        // Check01 - Depth
        if (node.depth == maxDepth) return;

        // Check02 - Divided already
        if ((node.leftNode != null) && (node.rightNode != null)) return;

        // Check03 - Can't divide because of area size limit
        if ((node.spaceArea.width < maxSpaceSize.x) && (node.spaceArea.height < maxSpaceSize.y)) return;

        // whether width is longer than height
        node.dividedHorizontally = (node.spaceArea.width > node.spaceArea.height) ? true : false;

        float slice = Random.Range(minDevideRate, maxDevideRate);

        // Step01 - Slice and Assign
        Node left;
        Node right;
        if (node.dividedHorizontally)
        {
            left = new Node(new RectInt(node.spaceArea.x, node.spaceArea.y, (int)Mathf.Round(slice * node.spaceArea.width), node.spaceArea.height));
            right = new Node(new RectInt(node.spaceArea.x + (int)Mathf.Round(slice * node.spaceArea.width), node.spaceArea.y, (int)Mathf.Round(node.spaceArea.width * (1 - slice)), node.spaceArea.height));
        }
        else
        {
            left = new Node(new RectInt(node.spaceArea.x, node.spaceArea.y, node.spaceArea.width, (int)Mathf.Round(slice * node.spaceArea.height)));
            right = new Node(new RectInt(node.spaceArea.x, node.spaceArea.y + (int)Mathf.Round(slice * node.spaceArea.height), node.spaceArea.width, (int)Mathf.Round(node.spaceArea.height * (1 - slice))));
        }
        left.parNode = right.parNode = node;
        left.depth = right.depth = node.depth + 1;

        if ((left.spaceArea.width > maxSpaceSize.x) && (left.spaceArea.height > maxSpaceSize.y))
        {
            node.leftNode = left;
            DivideMap(node.leftNode);
        }

        if ((left.spaceArea.width > maxSpaceSize.x) && (left.spaceArea.height > maxSpaceSize.y))
        {
            node.rightNode = right;
            DivideMap(node.rightNode);
        }

        // TODO - if depth == specific value
        //      start : if depth increase -> float StopDivideChance is increase
    }

    // Set Room's Outline
    private void SetRoomsOutline(Node node)
    {
        // TODO : Think about Nesting lines on each adjacent nodes
        if ((node.leftNode == null) && (node.rightNode == null))
        {
            int roomWidth = Random.Range(minRoomSize.x, Mathf.Min(node.spaceArea.width, maxRoomSize.x));
            int roomHeight = Random.Range(minRoomSize.y, Mathf.Min(node.spaceArea.height, maxRoomSize.y));
            int roomX = node.spaceArea.x + Random.Range(0, node.spaceArea.width - roomWidth);
            int roomY = node.spaceArea.y + Random.Range(0, node.spaceArea.height - roomHeight);
            node.roomArea = new RectInt(roomX, roomY, roomWidth, roomHeight);
        }
        else
        {
            if (node.leftNode != null)
            {
                SetRoomsOutline(node.leftNode);
            }
            if (node.rightNode != null)
            {
                SetRoomsOutline(node.rightNode);
            }
        }
    }

    // TODO - Check the Stop Condition of Repeat
    private List<Node> GetLeafNodes(Node node)
    {
        List<Node> leaves = new List<Node>();

        while ((node.leftNode != null) || (node.rightNode != null))
        {
            if (node.leftNode != null)
            {
                leaves = GetLeafNodes(node.leftNode);
            }
            if (node.rightNode != null)
            {
                leaves = GetLeafNodes(node.rightNode);
            }
        }

        leaves.Add(node);
        return leaves;
    }

    // TODO - Change Return Type To Couple(Node, Node)
    private Dictionary<Node, List<Node>> GetAdjacentLeaf(List<Node> leaves)
    {
        Dictionary<Node, List<Node>> adjacent = new Dictionary<Node, List<Node>>();
        List<Node> values;

        for (int i = 0; i < leaves.Count; i++)
        {
            values = new List<Node>();
            for (int j = 0; j < leaves.Count; j++)
            {
                if(i == j) continue;

                if ((leaves[i].spaceArea.x == (leaves[j].spaceArea.x + leaves[j].spaceArea.width)) || ((leaves[i].spaceArea.x + leaves[i].spaceArea.width) == leaves[j].spaceArea.x) ||
                    (leaves[i].spaceArea.y == (leaves[j].spaceArea.y + leaves[j].spaceArea.height)) || ((leaves[i].spaceArea.y + leaves[j].spaceArea.height) == leaves[j].spaceArea.y))
                    values.Add(leaves[j]);
            }
            adjacent.Add(leaves[i], values);
        }

        return adjacent;
    }

    // this code generates corridor (called by warp)
    private void ConnectRooms(Dictionary<Node, List<Node>> adjacent)
    {
        // This have to avoid duplication
        HashSet<Node> already = new HashSet<Node>();
        List<Node> list;

        foreach (KeyValuePair<Node, List<Node>> kv in adjacent){
            list = kv.Value;

            // Check 01 - Duplicate?

            // Check 02 - Roop?
        }
    }

    private void BFS()
    {
        //
    }

    private void LocateSpecialRoom()
    {
        // TODO - Before Typing this script,
        //      Write "Room.cs" scipt

        // TODO - place special rooms(boss, start) at each end of longest route
        //  using BFS
    }

    // this code locates prefabs along the outline
    private void PlaceTilemapPrefabs()
    {
        //
    }
}
