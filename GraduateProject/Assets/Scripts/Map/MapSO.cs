using UnityEngine;

[CreateAssetMenu(fileName = "MapSO", menuName = "Scriptable Objects/MapSO")]
public class MapSO : ScriptableObject
{
    [field: Header("#Room Components Prefabs")]
    #region MAP_SPRITES
    [SerializeField] public GameObject[] topLeftWall { get; private set; }
    [SerializeField] public GameObject[] topRightWall { get; private set; }
    [SerializeField] public GameObject[] bottomLeftWall { get; private set; }
    [SerializeField] public GameObject[] bottomRightWall { get; private set; }
    // side
    [SerializeField] public GameObject[] LeftWall { get; private set; }
    [SerializeField] public GameObject[] RightWall { get; private set; }
    // ceiling and ground
    [SerializeField] public GameObject[] ground { get; private set; }
    [SerializeField] public GameObject[] ceiling { get; private set; }
    #endregion

    #region MAP_VARIABLES
    [field: Header("#Map Variables")]
    [SerializeField] public Vector2Int mapSize{ get; private set; }   //Total Size

    [SerializeField] public Vector2Int minSpaceSize{ get; private set; }
    [SerializeField] public Vector2Int maxSpaceSize { get; private set; }

    [SerializeField] public Vector2Int minRoomSize{ get; private set; }
    [SerializeField] public Vector2Int maxRoomSize { get; private set; }

    //TODO - Two Type of Rate (Wide or Tall)
    [SerializeField] public float maxDevideRate{ get; private set; }
    [SerializeField] public float minDevideRate { get; private set; }

    [SerializeField] public int maxDepth { get; private set; }
    #endregion
}
