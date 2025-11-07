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
        // 영역이너무 작으면 return
        if (node.BSPArea.width < so.MinSpaceSize.x || node.BSPArea.height < so.MinSpaceSize.y) return;


        // 미리지정한 최대  깊이 도달 시 종료
        if (node.depth == so.MaxDepth) return;


        // 가로 세로 비율에 따라 어느 방향으로 분할할 지 결정
        node.dividedHorizontally = (node.BSPArea.width > node.BSPArea.height) ? true : false;
        float slice = Random.Range(so.MinDevideRate, so.MaxDevideRate);

        // 노드 분할 -> 이것도 미리 지정한 최대 or 최소 분할 비율을 적용
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

        left.parNode = right.parNode = node;
        left.depth = right.depth = node.depth + 1;

        // 노드 추가 및 재귀 호출
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
            leaves.AddRange(convertBSPIntoNode(node.leftNode));
        }
        if (node.rightNode != null)
        {
            leaves.AddRange(convertBSPIntoNode(node.rightNode));
        }

        if (node.leftNode == null && node.rightNode == null)
        {
            MapNode leaf = new MapNode();
            leaf.SpaceArea = node.BSPArea;
            leaves.Add(leaf);
        }
        return leaves;
    }
}
