using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 진행/맵 생성/보스필드/포탈/재시작 총괄
/// - InGameScene 로드시 PlayerManager.PreparePlayerForScene() 호출(단일 보장)
/// - 로드 직후 잠깐 대기 후 StartPoint로 텔레포트(맵-플레이어 타이밍 보정)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Managers
    public DataManager DataManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public PoolManager PoolManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }
    //public RankingManager RankingManager { get; private set; }
    public UIManager UIManager { get; private set; }

    public void RegisterUIManager(UIManager ui)
    {
        UIManager = ui;
#if UNITY_EDITOR
        Debug.Log("[GameManager] UIManager registered.");
#endif
    }

    // Stage Maps
    [Header("Stage MapSOs")]
    public List<MapSO> stages = new List<MapSO>();
    [Tooltip("1부터 시작하는 현재 스테이지 인덱스")]
    public int currentStage = 1;

    // Stage Transition Portal
    [Header("Stage Transition Portal")]
    public GameObject StagePortalPrefab;
    public string stagePortalResourcesPath = "Prefabs/Maps/Portal/Portal_Purple";

    private StageTransitionPortal _spawnedStagePortal;
    private bool _bossClearHandledThisStage = false;

    // BossField
    [Header("BossField (Runtime)")]
    public Transform bossFieldRootParent;
    private GameObject _currentBossField;

    // Internals
    private MapGenerator _mapGen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureRoomManager();
        EnsurePlayerManager();
        EnsureMapGenerator();

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (!StagePortalPrefab && !string.IsNullOrEmpty(stagePortalResourcesPath))
            StagePortalPrefab = Resources.Load<GameObject>(stagePortalResourcesPath);

        EnemyArchetypeRegistry.LoadAll(
            "SO/Stats/Enemies/Archtype",
            "SO/Stats/Enemies/Archetype",
            "Enemies/Archetypes",
            ""
        );
    }

    private void OnDestroy()
    {
        if (this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (RoomManager == null) EnsureRoomManager();
        if (PlayerManager == null) EnsurePlayerManager();
        if (_mapGen == null) EnsureMapGenerator();

        // ★ InGameScene 진입 시: Player 단일 보장 + 텔레포트 보정
        if (scene.name == PlayerManager?.gameplaySceneName)
        {
            PlayerManager.PreparePlayerForScene();
            StartCoroutine(Co_TeleportPlayerToStartAfterMap());
        }
    }

    private IEnumerator Co_TeleportPlayerToStartAfterMap()
    {
        // 맵 생성(Start 코루틴)과 타이밍을 맞추기 위해 몇 프레임 대기
        float t = 0.3f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            RoomManager?.TeleportToStart(PlayerManager.UnitRoot.transform);
    }

    // Ensure helpers
    private void EnsureRoomManager()
    {
        if (RoomManager == null) RoomManager = FindFirstObjectByType<RoomManager>();
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
        if (PlayerManager == null) PlayerManager = FindFirstObjectByType<PlayerManager>();
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

    private void EnsureMapGenerator()
    {
        if (_mapGen == null) _mapGen = FindFirstObjectByType<MapGenerator>();
    }

    // BossField API
    public Vector3? SpawnBossFieldAndGetSpawnPoint()
    {
        if (stages == null || stages.Count == 0 || currentStage < 1 || currentStage > stages.Count)
        {
            Debug.LogError("[GameManager] SpawnBossFieldAndGetSpawnPoint: invalid stage index.");
            return null;
        }

        var so = stages[currentStage - 1];
        if (!so || !so.BossFieldPrefab)
        {
            Debug.LogError("[GameManager] MapSO.BossFieldPrefab is not assigned.");
            return null;
        }

        if (_currentBossField)
        {
#if UNITY_EDITOR
            DestroyImmediate(_currentBossField);
#else
            Destroy(_currentBossField);
#endif
            _currentBossField = null;
        }

        Transform parent = bossFieldRootParent;
        if (!parent && RoomManager != null && RoomManager.Grid != null)
            parent = RoomManager.Grid.transform;

        _currentBossField = Instantiate(so.BossFieldPrefab, parent);

        Transform spawn = null;
        var trs = _currentBossField.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs) { if (t.CompareTag("BossPlayerSpawn")) { spawn = t; break; } }
        if (!spawn)
            foreach (var t in trs) { if (t.name.Equals("SpawnPoint")) { spawn = t; break; } }

        if (!spawn)
        {
            Debug.LogWarning("[GameManager] BossPlayerSpawn/SpawnPoint not found. Using prefab root.");
            return _currentBossField.transform.position;
        }
        return spawn.position;
    }

    public void ClearBossField()
    {
        if (_currentBossField)
        {
#if UNITY_EDITOR
            DestroyImmediate(_currentBossField);
#else
            Destroy(_currentBossField);
#endif
            _currentBossField = null;
        }
    }

    // Boss Clear Hook
    public void OnBossCleared(Room bossRoom)
    {
        if (_bossClearHandledThisStage) return;
        _bossClearHandledThisStage = true;

        bool isFinal = (stages != null && stages.Count > 0) ? (currentStage >= stages.Count) : true;
        if (isFinal)
        {
            UIManager?.ShowClearPanel();
            return;
        }

        TrySpawnStageTransitionPortal(bossRoom);
    }

    private void TrySpawnStageTransitionPortal(Room bossRoom)
    {
        if (_spawnedStagePortal != null) return;

        if (!StagePortalPrefab && !string.IsNullOrEmpty(stagePortalResourcesPath))
            StagePortalPrefab = Resources.Load<GameObject>(stagePortalResourcesPath);

        if (!StagePortalPrefab)
        {
            Debug.LogError("[GameManager] StageTransition Portal Prefab missing.");
            return;
        }

        Vector3 pos = bossRoom ? bossRoom.GetSpawnPosition()
                               : (PlayerManager?.UnitRoot ? PlayerManager.UnitRoot.transform.position : Vector3.zero);

        Transform parent = bossRoom ? bossRoom.transform
                                    : (RoomManager != null && RoomManager.Grid != null ? RoomManager.Grid.transform : null);

        var portalGO = Instantiate(StagePortalPrefab, pos, Quaternion.identity, parent);
        portalGO.name = "Portal_Purple (NextStage)";

        if (!portalGO.TryGetComponent<Collider2D>(out var col))
        {
            var bc = portalGO.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
        }

        if (!portalGO.TryGetComponent<StageTransitionPortal>(out var stp))
            stp = portalGO.AddComponent<StageTransitionPortal>();

        _spawnedStagePortal = stp;
#if UNITY_EDITOR
        Debug.Log($"[GameManager] StageTransitionPortal spawned once at {pos}.");
#endif
    }

    public void ResetStageClearFlags()
    {
        _bossClearHandledThisStage = false;
        _spawnedStagePortal = null;
    }

    // Stage Flow
    public void AdvanceToNextStage()
    {
        StartCoroutine(Co_AdvanceToNextStage());
    }

    private IEnumerator Co_AdvanceToNextStage()
    {
        if (_mapGen == null) { EnsureMapGenerator(); if (_mapGen == null) { Debug.LogError("[GameManager] Missing MapGenerator."); yield break; } }

        if (RoomManager != null)
            yield return RoomManager.Co_ResetRooms(true);

        ClearBossField();
        ResetStageClearFlags();

        currentStage = Mathf.Min(currentStage + 1, Mathf.Max(1, stages.Count));

        var next = stages[currentStage - 1];
        if (!next) { Debug.LogError($"[GameManager] MapSO for stage {currentStage} is null."); yield break; }

        _mapGen.Generate(next);

        // 플레이어 확보 & 텔레포트 보정
        PlayerManager?.PreparePlayerForScene(); // 혹시 모를 누락 대비
        yield return null;
        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            RoomManager?.TeleportToStart(PlayerManager.UnitRoot.transform);

        Debug.Log($"[GameManager] Advanced to Stage {currentStage}.");
    }

    // Restart
    public void RestartRun()
    {
        StartCoroutine(Co_RestartRun());
    }

    private IEnumerator Co_RestartRun()
    {
        if (_mapGen == null) { EnsureMapGenerator(); if (_mapGen == null) { Debug.LogError("[GameManager] Missing MapGenerator."); yield break; } }

        UIManager?.HideAll();

        if (RoomManager != null)
            yield return RoomManager.Co_ResetRooms(true);

        ClearBossField();
        ResetStageClearFlags();

        var so = (stages != null && stages.Count >= currentStage) ? stages[currentStage - 1] : null;
        if (!so) { Debug.LogError($"[GameManager] MapSO for stage {currentStage} is null."); yield break; }

        _mapGen.Generate(so);

        // 플레이어 확보 & 텔레포트 보정
        PlayerManager?.PreparePlayerForScene(); // 혹시 모를 누락 대비
        yield return null;
        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            RoomManager?.TeleportToStart(PlayerManager.UnitRoot.transform);

        PlayerManager?.Revive();

        Debug.Log($"[GameManager] Restarted Stage {currentStage}.");
    }
}
