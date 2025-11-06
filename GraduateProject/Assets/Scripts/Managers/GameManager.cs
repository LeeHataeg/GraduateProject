using System.Collections;
using System.Collections.Generic;
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

    // ====== [추가] 스테이지 흐름 ======
    [Header("Stage MapSOs")]
    public List<MapSO> stages = new List<MapSO>();

    [Header("Stage Portal Prefab")]
    [Tooltip("보스 격파 후 생성될 '스테이지 전환 포탈' 프리팹 (특수 색상/형태)")]
    public GameObject StagePortalPrefab;
    [Tooltip("Resources 경로 폴백 (에디터에서 직접 할당 가능)")]
    public string stagePortalResourcesPath = "Resources/Maps/Portal/Portal_Purple";

    [Header("Stage State (Runtime)")]
    [Tooltip("현재 스테이지 인덱스 (1 → 2)")]
    public int currentStage = 1;
    private bool _bossClearHandledThisStage = false;
    private StageTransitionPortal _spawnedStagePortal;

    private MapGenerator _mapGen;
    [Header("Runtime BossField")]
    public Transform bossFieldRootParent; // 없으면 RoomsRoot로 대체
    private GameObject _currentBossField;

    /// <summary>
    /// 현재 스테이지(MapSO 기준)의 BossField 프리팹을 생성하고,
    /// BossPlayerSpawn 태그(또는 이름 SpawnPoint)를 찾아 그 위치를 반환.
    /// </summary>
    public Vector3? SpawnBossFieldAndGetSpawnPoint()
    {
        var so = stages[currentStage - 1];
        if (!so || !so.BossFieldPrefab)
        {
            Debug.LogError("[GameManager] MapSO.BossFieldPrefab is not assigned.");
            return null;
        }

        // 기존 보스필드 제거
        if (_currentBossField)
        {
#if UNITY_EDITOR
            DestroyImmediate(_currentBossField);
#else
            Destroy(_currentBossField);
#endif
            _currentBossField = null;
        }

        _currentBossField = Instantiate(so.BossFieldPrefab, RoomManager.Grid.transform);

        // BossPlayerSpawn → 스폰지점 탐색
        Transform spawn = null;
        var trs = _currentBossField.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs)
        {
            if (t.CompareTag("BossPlayerSpawn")) { spawn = t; break; }
        }
        if (!spawn)
        {
            foreach (var t in trs)
            {
                if (t.name.Equals("SpawnPoint")) { spawn = t; break; }
            }
        }

        if (!spawn)
        {
            Debug.LogWarning("[GameManager] BossPlayerSpawn/SpawnPoint not found in BossField prefab. Use prefab root (0,0).");
            return Vector3.zero;
        }

        return spawn.position;
    }

    /// <summary>
    /// 생성된 BossField 제거(보스전 종료 시 등)
    /// </summary>
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
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 필요한 매니저 보장
        EnsureRoomManager();
        EnsurePlayerManager();   // ← 추가
        EnsureMapGenerator();    // ← 추가

        SceneManager.sceneLoaded += OnSceneLoaded;

        // 포탈 프리팹 폴백 로드
        if (!StagePortalPrefab && !string.IsNullOrEmpty(stagePortalResourcesPath))
        {
            StagePortalPrefab = Resources.Load<GameObject>(stagePortalResourcesPath);
        }

        EnemyArchetypeRegistry.LoadAll(
    "SO/Stats/Enemies/Archtype",   // ← 너의 실제 경로(철자 그대로)
    "SO/Stats/Enemies/Archetype",  // 백업 경로
    "Enemies/Archetypes",          // 다른 경로(있다면)
    ""                             // 최후의 폴백(전체 스캔)
);
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
        if (_mapGen == null) EnsureMapGenerator();

        // InGameScene 진입 시 Stage1이면 MapGenerator가 자체 Start()에서 Stage1SO로 그려줄 것임.
        // (MapGenerator의 Start는 mapSO(인스펙터)로 1회 자동 생성)
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

    private void EnsureMapGenerator()
    {
        if (_mapGen == null)
            _mapGen = FindFirstObjectByType<MapGenerator>();
        // MapGenerator는 씬 내 컴포넌트(DDOL 아님). 못찾으면 Start에서 자연 생성됨.
    }

    public void OnBossCleared(Room bossRoom)
    {
        // === (A) 스테이지당 1회만 처리 ===
        if (_bossClearHandledThisStage) return;
        _bossClearHandledThisStage = true;

        // 마지막 스테이지?
        bool isFinal = (stages != null && stages.Count > 0) ? (currentStage >= stages.Count) : true;
        if (isFinal)
        {
            UIManager?.ShowClearPanel();
            return;
        }

        // 다음 스테이지 전환 포탈 스폰
        TrySpawnStageTransitionPortal(bossRoom);
    }

    private void TrySpawnStageTransitionPortal(Room bossRoom)
    {
        // === (B) 이미 스폰된 전환 포탈이 있으면 재생성 금지 ===
        if (_spawnedStagePortal != null) return;

        if (!StagePortalPrefab)
        {
            // Resources 경로는 "Resources/" 접두사 없이!
            if (!string.IsNullOrEmpty(stagePortalResourcesPath))
                StagePortalPrefab = Resources.Load<GameObject>(stagePortalResourcesPath);
        }
        if (!StagePortalPrefab)
        {
            Debug.LogError("[GameManager] StageTransition Portal Prefab missing.");
            return;
        }

        // 위치 폴백(보스룸 중심 → 플레이어 → (0,0))
        Vector3 pos;
        if (bossRoom != null) pos = bossRoom.GetSpawnPosition();
        else if (PlayerManager && PlayerManager.UnitRoot) pos = PlayerManager.UnitRoot.transform.position;
        else pos = Vector3.zero;

        // 부모 폴백(보스룸 → RoomsRoot(Grid) → 씬 루트)
        Transform parent = bossRoom ? bossRoom.transform :
            (RoomManager != null && RoomManager.Grid != null ? RoomManager.Grid.transform : null);

        var portalGO = Instantiate(StagePortalPrefab, pos, Quaternion.identity, parent);
        portalGO.name = "Portal_Purple (NextStage)";

        if (!portalGO.TryGetComponent<Collider2D>(out var col))
        {
            var bc = portalGO.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
        }

        if (!portalGO.TryGetComponent<StageTransitionPortal>(out var stp))
            stp = portalGO.AddComponent<StageTransitionPortal>();

        _spawnedStagePortal = stp; // 이후 중복 방지

#if UNITY_EDITOR
        Debug.Log($"[GameManager] StageTransitionPortal spawned once at {pos}.");
#endif
    }
    // ====== [핵심] Stage2로 전환 ======
    // GameManager.cs (관련 메서드만 교체/추가)

    public void AdvanceToNextStage()
    {
        // 트리거/물리 콜백 중 호출되어도 안전하게 코루틴으로 처리
        StartCoroutine(Co_AdvanceToNextStage());
    }

    private IEnumerator Co_AdvanceToNextStage()
    {
        var rm = RoomManager;
        if (_mapGen == null) { Debug.LogError("[GameManager] Missing MapGenerator."); yield break; }

        yield return rm.Co_ResetRooms(true);

        // ★ 리셋 포인트
        _bossClearHandledThisStage = false;
        _spawnedStagePortal = null;

        currentStage = Mathf.Min(currentStage + 1, stages.Count);

        var next = stages[currentStage - 1];
        if (!next) { Debug.LogError($"[GameManager] MapSO for stage {currentStage} is null."); yield break; }

        _mapGen.Generate(next);

        yield return null;
        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            rm.TeleportToStart(PlayerManager.UnitRoot.transform);

        Debug.Log($"[GameManager] Advanced to Stage {currentStage}.");
    }
}
