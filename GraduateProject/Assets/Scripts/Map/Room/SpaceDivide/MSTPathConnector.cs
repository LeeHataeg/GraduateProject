using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using static Define;
using Edge = Define.Edge;

public class MSTPathConnector
{
    public List<MapNode> GetMSTPath(Dictionary<MapNode, List<MapNode>> adjacent)
    {
        List<Edge> edges = setEdge(adjacent);
        return constructMST(edges, adjacent.Count);
    }

    private List<Edge> setEdge(Dictionary<MapNode, List<MapNode>> adjacent)
    {
        List<Edge> edges = new List<Edge>();
        // Step01 - MST(kruskal)
        foreach (var kv in adjacent)
        {
            MapNode node = kv.Key;
            foreach (MapNode neighbor in kv.Value)
            {
                // Defending Duplicated Connections
                if (node.Id < neighbor.Id)
                {
                    edges.Add(new Edge(node, neighbor));
                }
            }
        }
        edges.Sort();
        return edges;
    }

    private List<MapNode> constructMST(List<Edge> edges, int count)
    {
        UnionFind uf = new UnionFind(count);
        List<Edge> mst = new List<Edge>();
        List<MapNode> result = new List<MapNode>();

        foreach (Edge edge in edges)
        {
            if (uf.Union(edge.Start.Id, edge.End.Id))
            {
                mst.Add(edge);
                updatePortals(edge.Start, edge.End, result);
            }
        }

        return result;
    }

    // TODO - Later : Random Bridge
    private void randomBridge()
    {
        // 
    }

    private void updatePortals(MapNode start, MapNode end, List<MapNode> result)
    {
        // TODO - Change Output Structure Into Continues
        int sInd = result.IndexOf(start);
        int eInd = result.IndexOf(end);

        if (sInd == -1)
        {
            result.Add(start);
            sInd = result.IndexOf(start); // 추가된 위치 업데이트
        }
        if (eInd == -1)
        {
            result.Add(end);
            eInd = result.IndexOf(end); // 추가된 위치 업데이트
        }

        Vector2 startCenter = start.SpaceArea.center;

        Vector2 endCenter = end.SpaceArea.center;

        float deltaX = endCenter.x - startCenter.x;
        float deltaY = endCenter.y - startCenter.y;


        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
        {
            if (deltaX > 0)
            {
                start.Portals.Add(new PortalInfo(PortalDir.right, end));
                end.Portals.Add(new PortalInfo(PortalDir.left, start));
            }
            else
            {
                start.Portals.Add(new PortalInfo(PortalDir.left, end));
                end.Portals.Add(new PortalInfo(PortalDir.right, start));
            }
        }
        else
        {
            if (deltaY > 0)
            {
                start.Portals.Add(new PortalInfo(PortalDir.down, end));
                end.Portals.Add(new PortalInfo(PortalDir.up, start));
            }
            else
            {
                start.Portals.Add(new PortalInfo(PortalDir.up, end));
                end.Portals.Add(new PortalInfo(PortalDir.down, start));
            }
        }
    }
}
