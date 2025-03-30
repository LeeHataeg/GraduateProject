using UnityEngine;

public enum RoomType
{
    Normal,
    Start,
    Boss,
    SemiBoss,
    Shop,
}

// Room Basic Info Save Class
// ex) tiles, passage, position of element
// I'll use 'Node' class
public class RoomData
{
    #region ROOM_CONFIGURE_VARIABLES
    public RoomType RoomType;

    // TODO - Enemy Spawn Info && ItemSpawnInfo
    #endregion

    #region ROOM_IMPLEMENTATION_VARIABLES
    public Node Node;

    public RectInt RoomSpace;
    #endregion

    #region ROOM_INGAME_VARIBALES
    // I have no idea about using this val
    public string SceneName;

    public bool isCleared;
    #endregion
    
    public RoomData(Node node)
    {
        Node = node;
        isCleared = false;
    }
}
