using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 기존 매니저 (필요하면 채워서 사용)
    public DataManager DataManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public PoolManager PoolManager { get; private set; }

    // ★ 반드시 보장
    public RoomManager RoomManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }  // ← 누락됐던 부분 복구

    // ★ 예전 코드 호환: UIManager 및 등록 메서드
    public UIManager UIManager { get; private set; }
    public void RegisterUIManager(UIManager ui)
    {
        UIManager = ui;
#if UNITY_EDITOR
        Debug.Log("[GameManager] UIManager registered.");
#endif
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 필요한 매니저 보장
        EnsureRoomManager();
        EnsurePlayerManager();   // ← 추가

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 후에도 항상 보장
        if (RoomManager == null) EnsureRoomManager();
        if (PlayerManager == null) EnsurePlayerManager();
    }

    private void EnsureRoomManager()
    {
        if (RoomManager == null)
            RoomManager = FindFirstObjectByType<RoomManager>();

        if (RoomManager == null)
        {
            var go = new GameObject("RoomManager");
            RoomManager = go.AddComponent<RoomManager>();
            DontDestroyOnLoad(go);
#if UNITY_EDITOR
            Debug.Log("[GameManager] RoomManager created on the fly.");
#endif
        }
    }

    private void EnsurePlayerManager()
    {
        if (PlayerManager == null)
            PlayerManager = FindFirstObjectByType<PlayerManager>();

        if (PlayerManager == null)
        {
            var go = new GameObject("PlayerManager");
            PlayerManager = go.AddComponent<PlayerManager>();
            DontDestroyOnLoad(go);
#if UNITY_EDITOR
            Debug.Log("[GameManager] PlayerManager created on the fly.");
#endif
        }
    }
}
