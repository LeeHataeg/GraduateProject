using System;
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

public class Room : MonoBehaviour
{
    public MapNode Node;
    public RectInt RoomSpace { get; private set; }

    public RoomType Type { get; private set; }
    public RoomState RoomState { get; private set; }
    public SpawnerController spawnManager { get; private set; }
    public PortalConnection PortalConnection { get; private set; }

    public void Initialize(RoomInitData init)
    {
        Node = init.Node;
        RoomSpace = init.RoomSpace;

        Type = init.RoomType;

        RoomState = gameObject.AddComponent<RoomState>();
        spawnManager = gameObject.AddComponent<SpawnerController>();
        PortalConnection = gameObject.AddComponent<PortalConnection>();

        PortalConnection.Initialize(init.Node.Portals);
        spawnManager.Initialize(init.RoomSpace);
    }
    public Vector2 GetSpawnPosition()
    {
        Vector2 lowerLeft = (Vector2)transform.position;
        return lowerLeft + new Vector2(RoomSpace.width * 0.5f,
                                       RoomSpace.height * 0.5f);
    }
}