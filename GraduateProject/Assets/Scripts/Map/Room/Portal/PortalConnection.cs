// PortalConnection.cs
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PortalConnection : MonoBehaviour
{
    // 최종적으로 채워질, 방향  → 방 인스턴스 매핑
    public Dictionary<PortalDir, Room> ConnectedRooms { get; private set; }
        = new Dictionary<PortalDir, Room>();

    // (추가) 방향 → 연결된 MapNode 매핑
    public Dictionary<PortalDir, MapNode> PortalInfos { get; private set; }
        = new Dictionary<PortalDir, MapNode>();

    // 맵 생성 단계에서 한 번 호출
    public void Initialize(List<PortalInfo> portalInfos)
    {
        foreach (var info in portalInfos)
        {
            if (!ConnectedRooms.ContainsKey(info.dir))
            {
                ConnectedRooms.Add(info.dir, null);
                PortalInfos.Add(info.dir, info.connected);
            }
        }
    }

    // runtime에 실제 Room 인스턴스를 연결
    public void ConnectRoom(PortalDir dir, Room room)
    {
        if (ConnectedRooms.ContainsKey(dir))
            ConnectedRooms[dir] = room;
    }
}
