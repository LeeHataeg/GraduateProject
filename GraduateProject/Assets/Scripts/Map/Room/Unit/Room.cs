using System;
using System.Collections.Generic;
using UnityEngine;


public class Room : MonoBehaviour
{
    public MapNode Node;

    public RectInt RoomSpace;



    public RoomType Type { get; private set; }
    public RoomState RoomState { get; private set; }
    public PortalConnection PortalConnection { get; private set; }
    public SpawnerController spawnManager { get; private set; }

    public void Initialize(RoomInitData init)
    {
        Node = init.Node;
        RoomSpace = init.RoomSpace;

        Type = init.RoomType;

        RoomState = gameObject.AddComponent<RoomState>();
        PortalConnection = gameObject.AddComponent<PortalConnection>();

        spawnManager = gameObject.GetComponent<SpawnerController>();

        PortalConnection.Initialize(init.Node.Portals);
    }
    public Vector2 GetSpawnPosition()
    {
        Vector2 lowerLeft = (Vector2)transform.position;
        Vector2 middle =  lowerLeft + new Vector2(RoomSpace.width * 0.5f,
                                       RoomSpace.height * 0.5f);
        Vector2 rightTop = lowerLeft + new Vector2(RoomSpace.width,
                                       RoomSpace.height);
        Debug.Log("RoomName : " + gameObject.name);
        Debug.Log("lowerLeft : " + lowerLeft);
        Debug.Log("middle : " + middle);
        Debug.Log("rightTop : " + rightTop);
        return middle;
    }
    public void OnPlayerEnter()
    {
        if (Type == RoomType.Normal && !RoomState.IsCleared)
        {
            spawnManager.SpawnEnemies();
        }
    }
}