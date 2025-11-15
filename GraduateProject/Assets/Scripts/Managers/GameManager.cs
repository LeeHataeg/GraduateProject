using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 겜 시작, 진행, 맵 생성 호출, 보스 필드 재시작 등 담당
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Managers
    public DataManager DataManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }
    public UIManager UIManager { get; private set; }

    public void RegisterUIManager(UIManager ui)
    {
        UIManager = ui;
#if UNITY_EDITOR
        Debug.Log("[GameManager] UIManager registered.");
#endif
    }

    // 스테이지 맵 (MapSO)을 리스트로 관리
    //이때ㅑ 각 인덱스는 stage 순서
    [Header("Stage별 MapSO (순서 주의)")]
    public List<MapSO> Stages = new List<MapSO>();
    public int CurrentStage = 1;    // 이 변수는 현재 인덱스

    // 스테이지 전송 포탈 에셋
    [Header("스테이지 전송 포탈")]
    public GameObject StagePortalPrefab;

    private StageTransitionPortal stagePortal;
    private bool isBossCleared = false;

    [Header("BossField 관련 변수 (Runtime 체크용)")]
    public Transform BossFieldRoot;
    private GameObject curBossField;

    private MapGenerator mapGen;

    [Header("TODO - 풀매니저 적용 때릴 것")]
    public GameObject PoolObjects;

    private void Awake()
    {
        // 싱글톤 ㅇㅇ
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // NULL 예외 처리
        EnsureRoomManager();
        EnsurePlayerManager();
        EnsureMapGenerator();

        // 씬 전환에 대해 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 스테이지 전환 포탈 NULL 예외 처리
        if (!StagePortalPrefab && !string.IsNullOrEmpty(Const.Prefabs_Purple_Portal))
            StagePortalPrefab = Resources.Load<GameObject>(Const.Prefabs_Purple_Portal);

        // Enemy 프리팹과 기타 SO들 로드
        //  아래는 그 예상(혹여나 경로명ㅇㅣ 다를 까봐) 경로들
        EnemyArchetypeRegistry.LoadAll(
            "SO/Stats/Enemies/Archtype",
            "SO/Stats/Enemies/Archetype",
            "Enemies/Archetypes",
            ""
        );

        if(PoolObjects == null)
        {
            PoolObjects = new GameObject("PoolObjects");
        }
    }

    private void OnDestroy()
    {
        if (this == null)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (RoomManager == null) 
            EnsureRoomManager();
        if (PlayerManager == null) 
            EnsurePlayerManager();
        if (mapGen == null) 
            EnsureMapGenerator();

        // "InGameScene" 진입 췤
        if (scene.name == Const.Scene_InGame)
        {
            PlayerManager.PreparePlayerObj();  // Player 단독 보장
            PlayerManager.SpawnToStartPoint();   //. Playger 스폰 위치 지정.
        }
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
        }
    }

    private void EnsureMapGenerator()
    {
        if (mapGen == null)
            mapGen = FindFirstObjectByType<MapGenerator>();

        // TODO - 이것도 없으면 만들어야함.
    }

    // 보스 전투 필드 생성 기능 + 보스 방의 플레이어가 스폰될 위치 리턴
    public Vector3? SpawnBossFieldAndGetSpawnPoint()
    {
        if (Stages == null || Stages.Count == 0 || CurrentStage < 1 || CurrentStage > Stages.Count)
        {
            Debug.LogError("[GameManager] SpawnBossFieldAndGetSpawnPoint: invalid stage index.");
            return null;
        }

        var so = Stages[CurrentStage - 1];
        if (!so || !so.BossFieldPrefab)
        {
            Debug.LogError("[GameManager] MapSO.BossFieldPrefab is not assigned.");
            return null;
        }

        // 기존(이전 스테이지)꺼가 남아있으면 제거
        if (curBossField)
        {
#if UNITY_EDITOR
            DestroyImmediate(curBossField);
#else
            Destroy(_currentBossField);
#endif
            curBossField = null;
        }

        Transform parent = BossFieldRoot;
        if (!parent && RoomManager != null && RoomManager.Grid != null)
            parent = RoomManager.Grid.transform;

        curBossField = Instantiate(so.BossFieldPrefab, parent);

        Transform spawn = null;
        var trs = curBossField.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs) { if (t.CompareTag("BossPlayerSpawn")) { spawn = t; break; } }
        if (!spawn)
            foreach (var t in trs) { if (t.name.Equals("SpawnPoint")) { spawn = t; break; } }

        if (!spawn)
        {
            Debug.LogWarning("[GameManager] BossPlayerSpawn/SpawnPoint not found. Using prefab root.");
            return curBossField.transform.position;
        }
        return spawn.position;
    }

    public void ClearBossField()
    {
        if (curBossField)
        {
#if UNITY_EDITOR
            DestroyImmediate(curBossField);
#else
            Destroy(_currentBossField);
#endif
            curBossField = null;
        }
    }

    public void OnBossCleared(Room bossRoom)
    {
        if (isBossCleared) return;
        isBossCleared = true;

        bool isFinal = (Stages != null && Stages.Count > 0) ? (CurrentStage >= Stages.Count) : true;
        if (isFinal)
        {
            UIManager?.ShowClearPanel();
            return;
        }

        TrySpawnStageTransitionPortal(bossRoom);
    }

    private void TrySpawnStageTransitionPortal(Room bossRoom)
    {
        if (stagePortal != null) return;

        if (!StagePortalPrefab && !string.IsNullOrEmpty(Const.Prefabs_Purple_Portal))
            StagePortalPrefab = Resources.Load<GameObject>(Const.Prefabs_Purple_Portal);

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

        stagePortal = stp;
#if UNITY_EDITOR
        Debug.Log($"[GameManager] StageTransitionPortal spawned once at {pos}.");
#endif
    }

    
    public void ResetStageClearFlags()
    {
        isBossCleared = false;
        stagePortal = null;
    }

    public void AdvanceToNextStage()
    {
        StartCoroutine(Co_AdvanceToNextStage());
    }

    private IEnumerator Co_AdvanceToNextStage()
    {
        if (mapGen == null) { EnsureMapGenerator(); if (mapGen == null) { Debug.LogError("[GameManager] Missing MapGenerator."); yield break; } }

        if (RoomManager != null)
            yield return RoomManager.Co_ResetRooms(true);

        ClearBossField();
        ResetStageClearFlags();

        CurrentStage = Mathf.Min(CurrentStage + 1, Mathf.Max(1, Stages.Count));

        var next = Stages[CurrentStage - 1];
        if (!next) { Debug.LogError($"[GameManager] MapSO for stage {CurrentStage} is null."); yield break; }

        mapGen.Generate(next);

        // 플레이어 확보 & 텔레포트 보정
        PlayerManager?.PreparePlayerObj(); // 혹시 모를 누락 대비
        yield return null;
        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            RoomManager?.TeleportToSpawnPoint(PlayerManager.UnitRoot.transform);

        Debug.Log($"[GameManager] Advanced to Stage {CurrentStage}.");
    }

    // Restart
    public void RestartRun()
    {
        StartCoroutine(Co_RestartRun());
    }

    private IEnumerator Co_RestartRun()
    {
        if (mapGen == null) { EnsureMapGenerator(); if (mapGen == null) { Debug.LogError("[GameManager] Missing MapGenerator."); yield break; } }

        UIManager?.HideAll();

        if (RoomManager != null)
            yield return RoomManager.Co_ResetRooms(true);

        ClearBossField();
        ResetStageClearFlags();

        var so = (Stages != null && Stages.Count >= CurrentStage) ? Stages[CurrentStage - 1] : null;
        if (!so) { Debug.LogError($"[GameManager] MapSO for stage {CurrentStage} is null."); yield break; }

        mapGen.Generate(so);

        // 플레이어 확보 & 텔레포트 보정
        PlayerManager?.PreparePlayerObj(); // 혹시 모를 누락 대비
        yield return null;
        if (PlayerManager != null && PlayerManager.UnitRoot != null)
            RoomManager?.TeleportToSpawnPoint(PlayerManager.UnitRoot.transform);

        PlayerManager?.Revive();

        Debug.Log($"[GameManager] Restarted Stage {CurrentStage}.");
    }
}
