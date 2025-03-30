using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Edge : IComparable<Edge>
{
    public Node Start { get; }
    public Node End { get; }
    public float Distance { get; }

    public Edge(Node start, Node end)
    {
        Start = start;
        End = end;
        Distance = Vector2.Distance(start.SpaceArea.position, end.SpaceArea.position);
    }

    public int CompareTo(Edge other)
    {
        return this.Distance.CompareTo(other.Distance);
    }
}

public class UnionFind
{
    private int[] parent;
    private int[] rank;

    public UnionFind(int size)
    {
        parent = new int[size];
        rank = new int[size];
        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }
    }

    public int Find(int u)
    {
        if (parent[u] != u)
        {
            parent[u] = Find(parent[u]);
        }
        return parent[u];
    }

    public bool Union(int u, int v)
    {
        int parentU = Find(u);
        int parentV = Find(v);

        // If Same Link? Union? Tree?
        if (parentU == parentV) return false;

        if (rank[parentU] > rank[parentV])
            parent[parentV] = parentU;
        else if (rank[parentU] < rank[parentV])
            parent[parentU] = parentV;
        else
        {
            parent[parentV] = parentU;
            rank[parentU]++;
        }
        return true;
    }
}

public class MSTPathConnector
{
    public List<Node> GetMSTPath(Dictionary<Node, List<Node>> adjacent)
    {
        List<Edge> edges = setEdge(adjacent);
        return constructMST(edges, adjacent.Count);
    }

    private List<Edge> setEdge(Dictionary<Node, List<Node>> adjacent)
    {
        List<Edge> edges = new List<Edge>();
        // Step01 - MST(kruskal)
        foreach (var kv in adjacent)
        {
            Node node = kv.Key;
            foreach (Node neighbor in kv.Value)
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

    private List<Node> constructMST(List<Edge> edges, int count)
    {
        UnionFind uf = new UnionFind(count);
        List<Edge> mst = new List<Edge>();
        List<Node> result = new List<Node>();

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

    private void updatePortals(Node start, Node end, List<Node> result)
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
                start.Portals.Add(new portalInfo(portalDir.right, end));
                end.Portals.Add(new portalInfo(portalDir.left, start));
            }
            else
            {
                start.Portals.Add(new portalInfo(portalDir.left, end));
                end.Portals.Add(new portalInfo(portalDir.right, start));
            }
        }
        else
        {
            if (deltaY > 0)
            {
                start.Portals.Add(new portalInfo(portalDir.down, end));
                end.Portals.Add(new portalInfo(portalDir.up, start));
            }
            else
            {
                start.Portals.Add(new portalInfo(portalDir.up, end));
                end.Portals.Add(new portalInfo(portalDir.down, start));
            }
        }
    }
}
