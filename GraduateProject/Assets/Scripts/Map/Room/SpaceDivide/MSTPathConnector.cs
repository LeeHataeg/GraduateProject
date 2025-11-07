using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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
        // 모든 Node에 대하여 수행
        foreach (var kv in adjacent)
        {
            MapNode node = kv.Key;
            // 각 노드 간 거리 비교
            foreach (MapNode neighbor in kv.Value)
            {
                // id 목적은 이중 foa문 중에 양방향으로 동일한 두 연결상태가 생길까봐
                if (node.Id < neighbor.Id)  
                {
                    edges.Add(new Edge(node, neighbor));
                }
            }
        }
        edges.Sort();
        return edges;
    }

    // 요 함수는 Kruskal 방식임
    //  -> UnionFind기법으로 이미 방문한 곳으로 복귀하는 것을 판정하고 간선이 N-1개가 되면 종료.
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
    private void updatePortals(MapNode start, MapNode end, List<MapNode> result)
    {
        // TODO - Change Output Structure Into Continues
        int sInd = result.IndexOf(start);
        int eInd = result.IndexOf(end);

        if (sInd == -1)
        {
            result.Add(start);
            sInd = result.IndexOf(start);
        }
        if (eInd == -1)
        {
            result.Add(end);
            eInd = result.IndexOf(end);
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

    // TODO - Later : Random Bridge
    private void randomBridge()
    {
        // 
    }
}
