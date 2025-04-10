using System.Collections.Generic;
using UnityEngine;


public class RoomState : MonoBehaviour
{
    public bool IsCleared { get; private set; }

    public void RoomCleared()
    {
        IsCleared = true;
    }
}

//Main frame Script of Ingame_Room
public class Room : MonoBehaviour
{
    public RoomType Type { get; private set; }

    public RoomState RoomState { get; private set; }
    public SpawnerController spawnManager { get; private set; }
    public PortalConnection PortalConnection { get; private set; }

    public void Initialize(RoomInitData init)
    {
        Type = init.RoomType;

        RoomState = gameObject.AddComponent<RoomState>();
        spawnManager = gameObject.AddComponent<SpawnerController>();
        PortalConnection = gameObject.AddComponent<PortalConnection>();

        PortalConnection.Initialize(init.Node.Portals);
        spawnManager.Initialize(init.RoomSpace);
    }
}