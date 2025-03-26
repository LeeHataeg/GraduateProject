using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class  Edge : IComparable<Edge>
{
    public Node Start { get; }
    public Node End { get; }
    public float Distance { get; }

    public Edge(Node start, Node end, float weight)
    {
        Start = start;
        End = end;
        Distance = weight;
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
    public Dictionary<int, Node> GetMSTPath(Dictionary<Node, List<Node>> adjacent)
    {
        //return Fuck U;
        List<Edge> edges= setEdge(adjacent);
        Dictionary<int, Node> result = connectRooms(edges, adjacent.Count);
        return result;
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
                if (node.id < neighbor.id)
                {
                    float weight = Vector2.Distance(node.spaceArea.position, neighbor.spaceArea.position);
                    edges.Add(new Edge(node, neighbor, weight));
                }
            }
        }

        return edges;
    }

    private Dictionary<int, Node> connectRooms(List<Edge> edges, int count)
    {
        edges.Sort();

        UnionFind uf = new UnionFind(count);
        List<Edge> mst = new List<Edge>();

        Dictionary<int, Node> result = new Dictionary<int, Node>();

        foreach (Edge edge in edges)
        {
            if (uf.Union(edge.Start.id, edge.End.id))
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

    private void updatePortals(Node start, Node end, Dictionary<int, Node> result)
    {
        Node tmp;

        Vector2 startCenter = new Vector2(
        start.spaceArea.x + start.spaceArea.width / 2,
        start.spaceArea.y + start.spaceArea.height / 2
        );

        Vector2 endCenter = new Vector2(
            end.spaceArea.x + end.spaceArea.width / 2,
            end.spaceArea.y + end.spaceArea.height / 2
        );

        float deltaX = endCenter.x - startCenter.x;
        float deltaY = endCenter.y - startCenter.y;

        if (result.ContainsKey(start.id))
        {
            result.TryGetValue(start.id, out tmp);
            result.Add(start.id, tmp);
        }
        if (result.ContainsKey(end.id))
        {
            result.TryGetValue(end.id, out tmp);
            result.Add(end.id, tmp);
        }

        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
        {
            if (deltaX > 0)
            {
                start.Portals.Add(new portalInfo(portalDir.right, end.id));
                end.Portals.Add(new portalInfo(portalDir.left, start.id));
            }
            else
            {
                start.Portals.Add(new portalInfo(portalDir.left, end.id));
                end.Portals.Add(new portalInfo(portalDir.right, start.id));
            }
            result.Add(start.id, start);
            result.Add(end.id, end);
        }
        else
        {
            if (deltaY > 0)
            {
                start.Portals.Add(new portalInfo(portalDir.down, end.id));
                end.Portals.Add(new portalInfo(portalDir.up, start.id));
            }
            else
            {
                start.Portals.Add(new portalInfo(portalDir.up, end.id));
                end.Portals.Add(new portalInfo(portalDir.down, start.id));
            }
            result.Add(start.id, start);
            result.Add(end.id, end);
        }
    }
}
