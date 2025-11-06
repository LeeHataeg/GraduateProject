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

    [Header("## Mask Tiles")] // 가리개
    [SerializeField] private Tile filledTile;
    [SerializeField] private Tile[] crackedTiles;
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

    [Header("Boss Field (Prefab)")]
    [Tooltip("이 스테이지에서 사용할 Boss 전용 전투 방 프리팹(여러 Tilemap 포함 가능)")]
    public GameObject BossFieldPrefab;

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

    public Tile FilledTile => filledTile;
    public Tile[] CrackedTiles => crackedTiles;
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
    #endregion
}
