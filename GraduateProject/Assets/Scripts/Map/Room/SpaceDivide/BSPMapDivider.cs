using System.Collections.Generic;
using UnityEngine;
using static Define;
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
    public List<MapNode> GetLeavesByBSP(MapSO so)
    {
        BSPNode bsp = new BSPNode(new RectInt(0, 0, so.MapSize.x, so.MapSize.y));
        bsp.depth = 0;

        divideMap(bsp, so);
        List<MapNode> list = convertBSPIntoNode(bsp);
        return list;
    }

    private void divideMap(BSPNode node, MapSO so)
    {
        // 1. 너무 작은 영역이면 종료
        if (node.BSPArea.width < so.MinSpaceSize.x || node.BSPArea.height < so.MinSpaceSize.y)
        {
            return;
        }

        // 2. 최대 깊이에 도달하면 종료
        if (node.depth == so.MaxDepth)
        {
            return;
        }

        // 3. 가로/세로 분할 결정
        node.dividedHorizontally = (node.BSPArea.width > node.BSPArea.height) ? true : false;
        float slice = Random.Range(so.MinDevideRate, so.MaxDevideRate);

        // 4. 노드 분할
        BSPNode left, right;
        if (node.dividedHorizontally)
        {
            int splitWidth = (int)Mathf.Round(slice * node.BSPArea.width);
            if (splitWidth < so.MinSpaceSize.x || node.BSPArea.width - splitWidth < so.MinSpaceSize.x) return;
            left = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y, splitWidth, node.BSPArea.height));
            right = new BSPNode(new RectInt(node.BSPArea.x + splitWidth, node.BSPArea.y, node.BSPArea.width - splitWidth, node.BSPArea.height));
        }
        else
        {
            int splitHeight = (int)Mathf.Round(slice * node.BSPArea.height);
            if (splitHeight < so.MinSpaceSize.y || node.BSPArea.height - splitHeight < so.MinSpaceSize.y) return;
            left = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y, node.BSPArea.width, splitHeight));
            right = new BSPNode(new RectInt(node.BSPArea.x, node.BSPArea.y + splitHeight, node.BSPArea.width, node.BSPArea.height - splitHeight));
        }

        // 5. 부모 연결 및 깊이 설정
        left.parNode = right.parNode = node;
        left.depth = right.depth = node.depth + 1;

        // 6. 노드 추가 및 재귀 분할
        node.leftNode = left;
        node.rightNode = right;

        divideMap(node.leftNode, so);
        divideMap(node.rightNode, so);
    }

    private List<MapNode> convertBSPIntoNode(BSPNode node)
    {
        List<MapNode> leaves = new List<MapNode>();

        if (node.leftNode != null)
        {
            leaves.AddRange(convertBSPIntoNode(node.leftNode)); // 기존 값 유지하면서 추가
        }
        if (node.rightNode != null)
        {
            leaves.AddRange(convertBSPIntoNode(node.rightNode)); // 기존 값 유지하면서 추가
        }

        // 리프 노드만 리스트에 추가
        if (node.leftNode == null && node.rightNode == null)
        {
            MapNode leaf = new MapNode();
            leaf.SpaceArea = node.BSPArea;
            leaves.Add(leaf);
        }
        return leaves;
    }
}
