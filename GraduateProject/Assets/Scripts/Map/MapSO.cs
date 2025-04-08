using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "MapSO", menuName = "Scriptable Objects/MapSO")]
public class MapSO : ScriptableObject
{
    #region MAP_SPRITES
    [Header("# Map Tiles")]
    [Header("## Walls")]
    [SerializeField] private Tile[] topLeftWall;
    [SerializeField] private Tile[] topRightWall;
    [SerializeField] private Tile[] bottomLeftWall;
    [SerializeField] private Tile[] bottomRightWall;

    [SerializeField] private Tile[] leftSideWall;
    [SerializeField] private Tile[] rightSideWall;
    [SerializeField] private Tile[] ground;
    [SerializeField] private Tile[] ceiling;

    [Header("## platforms")]
    [SerializeField] private Tile[] leftPlatforms;
    [SerializeField] private Tile[] rightPlatforms;
    [SerializeField] private Tile[] middlePlatforms;
    #endregion

    #region MAP_VARIABLES
    [Header("# Map Variables")]
    [SerializeField] private Vector2Int mapSize;   // Total Size
    [SerializeField] private Vector2Int minSpaceSize;
    [SerializeField] private Vector2Int maxSpaceSize;
    [SerializeField] private Vector2Int minRoomSize;
    [SerializeField] private Vector2Int maxRoomSize;
    [SerializeField] private float maxDevideRate;
    [SerializeField] private float minDevideRate;
    [SerializeField] private int maxDepth;
    #endregion

    #region ROOM_PREFABS
    [field: Header("#Room Prefabs")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject bossRoom;
    [SerializeField] private GameObject shopRoom;
    // TODO - Add Puzzle Maps
    //[SerializeField] private GameObject[] puzzleRooms;
    //[SerializeField] private GameObject midbossRoom;
    #endregion  

    #region GETTERS
    public Tile[] TopLeftWall => topLeftWall;
    public Tile[] TopRightWall => topRightWall;
    public Tile[] BottomLeftWall => bottomLeftWall;
    public Tile[] BottomRightWall => bottomRightWall;
    public Tile[] LeftWall => leftSideWall;
    public Tile[] RightWall => rightSideWall;
    public Tile[] Ground => ground;
    public Tile[] Ceiling => ceiling;

    public Tile[] LeftPlatforms => leftPlatforms;
    public Tile[] RightPlatforms => rightPlatforms;
    public Tile[] MiddlePlatforms => middlePlatforms;

    public Vector2Int MapSize => mapSize;
    public Vector2Int MinSpaceSize => minSpaceSize;
    public Vector2Int MaxSpaceSize => maxSpaceSize;
    public Vector2Int MinRoomSize => minRoomSize;
    public Vector2Int MaxRoomSize => maxRoomSize;
    public float MaxDevideRate => maxDevideRate;
    public float MinDevideRate => minDevideRate;
    public int MaxDepth => maxDepth;

    public GameObject StartRoom => startRoom;
    public GameObject BossRoom => bossRoom;
    public GameObject ShopRoom => shopRoom;
    #endregion
}
