using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static Define;

// Room Basic Info Save Class
// ex) tiles, passage, position of element
// I'll use 'Node' class
public class RoomInitData
{
    #region ROOM_CONFIGURE_VARIABLES
    public RoomType RoomType;

    // TODO - Enemy Spawn Info && ItemSpawnInfo
    #endregion

    #region ROOM_IMPLEMENTATION_VARIABLES
    public MapNode Node;

    public RectInt RoomSpace;
    #endregion
    
    public RoomInitData(MapNode node)
    {
        Node = node;
    }
}