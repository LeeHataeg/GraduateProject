//using System.Collections.Generic;
//using Unity.VisualScripting;
//using UnityEditor;
//using UnityEngine;

//public class PlayerStatController
//{
//    public class Edge
//    {
//        public Node Start { get; }
//        public Node End { get; }
//        public float Weight { get; }

//        public Edge(Node start, Node end, float weight)
//        {
//            Start = start;
//            End = end;
//            Weight = weight;
//        }
//    }

//    public class UnionFind
//    {
//        private int[] parent;
//        private int[] rank;

//        public UnionFind(int size)
//        {
//            parent = new int[size];
//            rank = new int[size];
//            for (int i = 0; i < size; i++)
//            {
//                parent[i] = i;  // 각 원소 부모를 자신으로 초기화
//                rank[i] = 0;    //초기 랭크 0
//            }
//        }

//        public int Find(int u)
//        {
//            if (parent[u] != u)
//            {
//                parent[u] = Find(parent[u]);
//            }
//            return parent[u];
//        }

//        public bool Union(int u, int v)
//        {
//            int rootU = Find(u);
//            int rootV = Find(v);

//            if (rootU == rootV) return false;

//            if (rank[rootU] > rank[rootV])
//                parent[rootV] = rootU;
//            else if (rank[rootU] < rank[rootV])
//                parent[rootU] = rootV;
//            else
//            {
//                parent[rootV] = rootU;
//                rank[rootU]++;
//            }
//            return true;
//        }
//    }

//    // this code generates corridor (called by warp)
//    private void ConnectRooms(Dictionary<Node, List<Node>> adjacent)
//    {
//        List<Edge> edges = new List<Edge>();
//        // Step01 - MST(kruskal)
//        foreach (var kv in adjacent)
//        {
//            Node node = kv.Key;
//            foreach (Node neighbor in kv.Value)
//            {
//                // 간선을 생성하되, 두 노드의 순서를 고려하여 중복 생성 방지
//                if (node.id < neighbor.id) // 예시로 id를 사용해 중복 방지
//                {
//                    // 가중치는 임의로 설정하거나 거리 등을 기반으로 계산
//                    float weight = Vector2.Distance(node.spaceArea.position, neighbor.spaceArea.position);
//                    edges.Add(new Edge(node, neighbor, weight));
//                }
//            }
//        }

//        edges.Sort((a, b) => a.Weight.CompareTo(b.Weight)); // 간선 가중치 기준으로 정렬

//        UnionFind uf = new UnionFind(adjacent.Count); // 노드 수에 맞게 초기화
//        List<Edge> mst = new List<Edge>(); // MST에 포함된 간선 리스트

//        foreach (Edge edge in edges)
//        {
//            // 두 노드가 같은 집합에 속하지 않는 경우 연결
//            if (uf.Union(edge.Start.id, edge.End.id))
//            {
//                mst.Add(edge); // MST에 추가
//                               // 실제 방 연결 로직을 여기에 추가
//                //ConnectRoomsInGame(edge.Start.id, edge.End.id);
//            }
//        }
//    }
//}