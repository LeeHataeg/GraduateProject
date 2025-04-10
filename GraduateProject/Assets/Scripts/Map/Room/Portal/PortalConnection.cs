using System.Collections.Generic;
using UnityEngine;

public class PortalConnection : MonoBehaviour
{
    public Dictionary<PortalDir, Room> ConnectedRooms { get; private set; } = new Dictionary<PortalDir, Room>();

    public void Initialize(List<PortalInfo> portalInfos)
    {
        foreach (var portalInfo in portalInfos)
        {
            if (!ConnectedRooms.ContainsKey(portalInfo.dir))
                ConnectedRooms.Add(portalInfo.dir, null);
        }
    }

    public void ConnectRoom(PortalDir dir, Room room)
    {
        if (ConnectedRooms.ContainsKey(dir))
            ConnectedRooms[dir] = room;
    }
}
