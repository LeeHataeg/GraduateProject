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

            Instance.allocate();
            Instance.init();
        }
    }

    #region MANAGERS

    public DataManager DataManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public PoolManager PoolManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }
    #endregion

    private void allocate()
    {
        AudioManager = gameObject.GetComponent<AudioManager>();
        UIManager = gameObject.GetComponent<UIManager>();
        RoomManager = gameObject.GetComponent<RoomManager>();
        PlayerManager = gameObject.GetComponent<PlayerManager>();
    }
    private void init()
    {
        if(AudioManager == null)
            AudioManager = Instance.gameObject.AddComponent<AudioManager>();
        if(UIManager == null)
            UIManager = Instance.gameObject.AddComponent<UIManager>();
        if(RoomManager == null)
            RoomManager = Instance.gameObject.AddComponent<RoomManager>();
        if(PlayerManager == null)
            PlayerManager = Instance.gameObject.AddComponent<PlayerManager>();
    }
}
