// PortalInitializer.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;
using System;
using Object = UnityEngine.Object;

public class PortalInitializer
{
    [Header("Addressables Key for Portal Prefab")]
    [SerializeField] private string portalAddress = "Portal";
    private GameObject portalPrefab;

    public PortalInitializer(string address = "Portal")
    {
        portalAddress = address;
    }
    // Addressable에서 Portal 프리팹 로드
    public void SetPortalPrefabAsync()
    {
        portalPrefab = (GameObject)Resources.Load("Prefabs/Maps/Portal");

        if (portalPrefab == null)
            throw new InvalidOperationException(
                $"Portal 프리팹 로드 실패: key={portalAddress}"
            );
    }

    public void Init(List<Room> rooms)
    {
        // MapNode.Id → Room 인스턴스 사전 구축
        var roomMap = new Dictionary<int, Room>();
        foreach (var room in rooms)
            roomMap[room.Node.Id] = room;

        // 각 Room별로 PortalInfos(방향 → MapNode)를 순회하며
        foreach (var room in rooms)
        {
            foreach (var kv in room.PortalConnection.PortalInfos)
            {
                var dir = kv.Key;
                var destNode = kv.Value;

                // 1) Portal 오브젝트 인스턴스화
                var pObj = Object.Instantiate(portalPrefab, room.transform);
                var portal = pObj.GetComponent<Portal>();
                portal.Initialize(room, dir);

                // 2) PortalConnection에 실제 Room 객체 연결
                if (roomMap.TryGetValue(destNode.Id, out var destRoom))
                    room.PortalConnection.ConnectRoom(dir, destRoom);

                // 3) Tilemap 경계에서 중앙 위치 계산 후 배치
                var tilemap = room.GetComponentInChildren<Tilemap>();
                tilemap.CompressBounds();
                var b = tilemap.cellBounds;
                Vector3 wMin = tilemap.transform.position + (Vector3)b.min;
                Vector3 wMax = tilemap.transform.position + (Vector3)b.max;
                
                Vector3 pos;
                switch (dir)
                {
                    case PortalDir.right:
                        pos = new Vector3(wMax.x - 1.5f, (wMin.y + wMax.y) * 0.5f, 0);
                        break;
                    case PortalDir.left:
                        pos = new Vector3(wMin.x + 1.5f, (wMin.y + wMax.y) * 0.5f, 0);
                        break;
                    case PortalDir.up:
                        pos = new Vector3((wMin.x + wMax.x) * 0.5f, wMax.y - 1.5f, 0);
                        break;
                    default: // down
                        pos = new Vector3((wMin.x + wMax.x) * 0.5f, wMin.y + 1.5f, 0);
                        break;
                }
                pObj.transform.position = pos;
            }

        }
    }
}