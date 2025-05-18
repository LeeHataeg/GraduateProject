using System.Data;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // This script Manages Operation of Game
    // Like Game Cycle( Gamestart,  etc )
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<GameManager>();

            if (Instance == null)
            {
                GameObject manager = new GameObject("GameManager");
                Instance = manager.AddComponent<GameManager>();
            }

            Instance.init();
            //DontDestroyOnLoad(instance);
        }
    }

    #region MANAGERS

    public DataManager DataManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public PoolManager PoolManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public PlayerManager1 PlayerManager { get; private set; }
    #endregion


    private void init()
    {
        AudioManager = Instance.gameObject.AddComponent<AudioManager>();
        UIManager = Instance.gameObject.AddComponent<UIManager>();
        RoomManager = Instance.gameObject.AddComponent<RoomManager>();
        PlayerManager = Instance.gameObject.AddComponent<PlayerManager1>();
    }
}
