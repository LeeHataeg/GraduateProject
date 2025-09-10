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

    // PortalInitializer.cs (Init 메서드의 foreach(room) 루프 끝 부분에 한 줄 추가)
    public void Init(List<Room> rooms)
    {
        var roomMap = new Dictionary<int, Room>();
        foreach (var room in rooms)
            roomMap[room.Node.Id] = room;

        foreach (var room in rooms)
        {
            foreach (var kv in room.PortalConnection.PortalInfos)
            {
                var dir = kv.Key;
                var destNode = kv.Value;

                var pObj = Object.Instantiate(portalPrefab, room.transform);
                var portal = pObj.GetComponent<Portal>();
                portal.Initialize(room, dir);

                if (roomMap.TryGetValue(destNode.Id, out var destRoom))
                    room.PortalConnection.ConnectRoom(dir, destRoom);

                var tilemap = room.GetComponentInChildren<Tilemap>();
                tilemap.CompressBounds();
                var b = tilemap.cellBounds;
                Vector3 wMin = tilemap.transform.position + (Vector3)b.min;
                Vector3 wMax = tilemap.transform.position + (Vector3)b.max;

                Vector3 pos = dir switch
                {
                    PortalDir.right => new Vector3(wMax.x - 1.5f, (wMin.y + wMax.y) * 0.5f, 0),
                    PortalDir.left => new Vector3(wMin.x + 1.5f, (wMin.y + wMax.y) * 0.5f, 0),
                    PortalDir.up => new Vector3((wMin.x + wMax.x) * 0.5f, wMax.y - 1.5f, 0),
                    _ => new Vector3((wMin.x + wMax.x) * 0.5f, wMin.y + 1.5f, 0),
                };
                pObj.transform.position = pos;
            }

            // ▼ 여기!
            room.CachePortals(); // 이 방의 모든 포탈 생성 완료 → 캐시
        }
    }

}