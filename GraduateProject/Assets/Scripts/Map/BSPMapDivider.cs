using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    #region ABOUT_NODE
    public BSPNode parNode;

    public BSPNode leftNode;
    public BSPNode rightNode;
    #endregion

    //public RectInt spaceArea;
    public RectInt BSPArea;

    public bool dividedHorizontally;

    public int depth;

    public BSPNode(RectInt rect)
    {
        BSPArea = rect;
    }
}

public class BSPMapDivider
{
    public List<Node> GetLeavesByBSP(MapSO so)
    {
        BSPNode bsp = new BSPNode(new RectInt(0, 0, so.MapSize.x, so.MapSize.y));
        bsp.depth = 0;

        divideMap(bsp, so);
        return convertBSPIntoNode(bsp);
    }

    private void divideMap(BSPNode node, MapSO so)
    {
        // Check01 - Depth
        if (node.depth == so.MaxDepth) return;

        // Check02 - Divided already
        if ((node.leftNode != null) && (node.rightNode != null)) return;

        // Check03 - Can't divide because of area size limit
        if ((node.BSPArea.width < so.MaxSpaceSize.x) && (node.BSPArea.height < so.MaxSpaceSize.y)) return;

        // whether width is longer than height
        node.dividedHorizontally = (node.BSPArea.width > node.BSPArea.height) ? true : false;

        float slice = Random.Range(so.MinDevideRate, so.MaxDevideRate);

        // Step01 - Slice and Assign
        BSPNode left;
        BSPNode right;
        if (node.dividedHorizontally)
        {
            left = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y, (int)Mathf.Round(slice * node.BSPArea.width), node.BSPArea.height));
            right = new BSPNode(new RectInt(node.BSPArea.x + (int)Mathf.Round(slice * node.BSPArea.width), node.BSPArea.y, (int)Mathf.Round(node.BSPArea.width * (1 - slice)), node.BSPArea.height));
        }
        else
        {
            left = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y, node.BSPArea.width, (int)Mathf.Round(slice * node.BSPArea.height)));
            right = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y + (int)Mathf.Round(slice * node.BSPArea.height), node.BSPArea.width, (int)Mathf.Round(node.BSPArea.height * (1 - slice))));
        }
        left.parNode = right.parNode = node;
        left.depth = right.depth = node.depth + 1;

        if ((left.BSPArea.width > so.MaxSpaceSize.x) && (left.BSPArea.height > so.MaxSpaceSize.y))
        {
            node.leftNode = left;
            divideMap(node.leftNode, so);
        }

        if ((left.BSPArea.width > so.MaxSpaceSize.x) && (left.BSPArea.height > so.MaxSpaceSize.y))
        {
            node.rightNode = right;
            divideMap(node.rightNode, so);
        }

        // TODO - Variety of Map Size
        // if depth == specific value
        //      start : if depth increase -> float StopDivideChance is increase
    }

    private List<Node> convertBSPIntoNode(BSPNode node)
    {
        List<Node> leaves = new List<Node>();
        Node leaf = new Node();

        if (node.leftNode != null)
        {
            leaves = convertBSPIntoNode(node.leftNode);
        }
        if (node.rightNode != null)
        {
            leaves = convertBSPIntoNode(node.rightNode);
        }

        leaf.SpaceArea = node.BSPArea;
        leaves.Add(leaf);

        return leaves;
    }
}
