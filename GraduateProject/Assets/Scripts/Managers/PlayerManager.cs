using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Prefab & Spawn")]
    public GameObject playerPrefab;         // 에디터에서 직결 or Resources
    public string playerResourcesPath = "Prefabs/Player/Player/Player";

    [Tooltip("인게임 씬 이름(없어도 동작하도록 보강되어 있음)")]
    public string gameplaySceneName = "InGameScene";

    [Header("Refs (Runtime)")]
    [Tooltip("클론 프리팹 최상위(캔버스 밑 RectTransform 등)")]
    public GameObject Player;               // RectTransform일 수 있음(움직이지 않음)
    [Tooltip("실제 물리 이동·충돌의 루트(이 오브젝트만 움직임)")]
    public GameObject UnitRoot;             // ★ 여기만 이동시킨다

    // 내부
    private Coroutine _fallbackCo;
    private bool _spawned;

    // 외부(UIManager 등) 연결 신호
    public event Action<EquipmentManager> OnEquipmentReady;

    private bool _resetLock;           // PlayerManager 필드
    private float _resetLockUntil;

    // 재시작 시 강제 신규 스폰 보장 & 디버깅용
    private bool _forceFreshSpawn;
    private bool _freshSpawnDoneThisScene;
    private int _spawnRunId;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 인스펙터 잔존 참조 초기화
        Player = null;
        UnitRoot = null;
        _spawned = false;
        _forceFreshSpawn = false;
        _freshSpawnDoneThisScene = false;
        _spawnRunId = 0;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 씬 로드 콜백을 놓친 경우 대비: 활성 씬 기준으로 한 프레임 뒤 스폰 시도
    private void Start()
    {
        StartCoroutine(Co_DeferFirstSpawn());
    }

    private IEnumerator Co_DeferFirstSpawn()
    {
        yield return null; // 한 프레임 대기
        if (!_spawned)
            TrySpawnInActiveScene();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TryUnsubscribeRoomEvent();
    }

    public void ResetState() => ResetState(true);

    public void ResetState(bool destroyPlayer = true)
    {
        // 🔧 1) 방금 스폰 직후의 안전구간(0.5s)엔 ResetState를 무시
        if (_resetLock && Time.realtimeSinceStartup < _resetLockUntil)
        {
            Debug.LogWarning("[PlayerManager] ResetState ignored due to resetLock window.");
            return;
        }

        // 이 매니저에서 돌고 있을 수 있는 코루틴 전부 중단
        StopAllCoroutines();

        // 현재 플레이어/루트 제거(있으면)
        if (destroyPlayer)
        {
            if (UnitRoot != null) Destroy(UnitRoot);
            if (Player != null) Destroy(Player);
        }

        Player = null;
        UnitRoot = null;
        _spawned = false;
        TryUnsubscribeRoomEvent();

        Debug.Log("[PlayerManager] ResetState: cleared current player. Will respawn in next gameplay scene.");
    }


    // ===== 공통 스폰 경로 =====
    private void TrySpawnInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        Debug.Log($"[PM] TrySpawnInActiveScene: '{scene.name}', _spawned={_spawned}, forceFresh={_forceFreshSpawn}");

        // 인게임 씬 필터 (필요 없으면 gameplaySceneName 빈 문자열로 두세요)
        if (!string.IsNullOrEmpty(gameplaySceneName) && scene.name != gameplaySceneName)
        {
            Debug.Log("[PM] Scene filtered by gameplaySceneName. Skip spawn.");
            return;
        }

        // RoomManager 우선
        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (rm == null)
        {
            SafeStopFallback();
            _fallbackCo = StartCoroutine(Co_WaitAndSpawnFallback());
            return;
        }

        if (rm.HasStartPoint)
        {
            Debug.Log("[PM] HasStartPoint=TRUE → PlayerInit(startPoint)");
            PlayerInit(rm.GetStartPoint());
        }
        else
        {
            Debug.Log("[PM] HasStartPoint=FALSE → subscribe + fallback");
            TrySubscribeRoomEvent(rm);
            SafeStopFallback();
            _fallbackCo = StartCoroutine(Co_FallbackIfNoStartPoint(0.2f));
        }
    }

    // ===== 씬 이벤트 래퍼 =====
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _freshSpawnDoneThisScene = false;
        Debug.Log($"[PM] OnSceneLoaded: '{scene.name}', mode={mode}, _spawned={_spawned}, forceFresh={_forceFreshSpawn}");

        if (_forceFreshSpawn)
        {
            // 우리가 쥐고 있던 것 제거(파괴 예약)
            if (UnitRoot != null) Destroy(UnitRoot);
            if (Player != null) Destroy(Player);
            UnitRoot = null;
            Player = null;
            _spawned = false;

            // 씬 전체/ DDOL에서 Player 류만 제거(파괴 예약)
            foreach (var go in scene.GetRootGameObjects())
                PurgePlayersRecursive(go.transform);

            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj == null) continue;
                if (obj == this.gameObject) continue;
                if (obj.scene.name != "DontDestroyOnLoad") continue;
                PurgePlayersRecursive(obj.transform);
            }

            // ★ 한 프레임 대기 후 스폰 시도 (Destroy가 실제 반영되고 난 뒤)
            StartCoroutine(Co_SpawnAfterFrame());
            return;
        }

        TrySpawnInActiveScene();
    }

    private IEnumerator Co_SpawnAfterFrame()
    {
        // 파괴 예약이 실제로 반영되도록 한 프레임 기다림
        yield return null;
        TrySpawnInActiveScene();
    }

    // Player 류만 제거: PlayerController 컴포넌트 or 태그 "Player"
    // 절대 삭제 금지: PlayerManager/GameManager/UIManager/RoomManager 및 자기 자신
    private void PurgePlayersRecursive(Transform t)
    {
        if (t == null) return;

        // 자기 자신 및 매니저류 제외
        if (ReferenceEquals(t.gameObject, this.gameObject)) return;
        if (t.GetComponent<PlayerManager>() != null) return;
        if (t.GetComponent<GameManager>() != null) return;
        if (t.GetComponent<UIManager>() != null) return;
        if (t.GetComponent<RoomManager>() != null) return;

        bool isPlayerLike =
            (t.GetComponent<PlayerController>() != null) ||
            t.CompareTag("Player");

        if (isPlayerLike)
        {
            Destroy(t.gameObject);  // 런타임은 Destroy 사용
            return; // 자식까지 볼 필요 없음 (같이 없어짐)
        }

        for (int i = 0; i < t.childCount; i++)
            PurgePlayersRecursive(t.GetChild(i));
    }

    public void PlayerInit(Vector2 spawnPos)
    {
        Debug.Log($"[PM] PlayerInit: spawned={_spawned}, forceFresh={_forceFreshSpawn}, spawnPos={spawnPos}");

        // 이번 씬에서 이미 신규 스폰을 끝냈다면 어떤 호출도 무시
        if (_freshSpawnDoneThisScene)
        {
            Debug.Log("[PM] PlayerInit ignored: fresh spawn already done in this scene.");
            return;
        }

        // 이미 스폰되어 있고 forceFresh가 아니면 무시(재배치 금지)
        if (_spawned && !_forceFreshSpawn)
        {
            Debug.Log("[PM] PlayerInit ignored: already spawned and not in force-fresh mode.");
            return;
        }

        // 프리팹 로드
        var prefab = playerPrefab != null ? playerPrefab : Resources.Load<GameObject>(playerResourcesPath);
        if (prefab == null)
        {
            var state = playerPrefab ? "SET" : "NULL";
            Debug.LogError($"[PM] Player prefab missing. Field={state}, Resources({playerResourcesPath})=NULL");
            return;
        }

        // 안전망: 잔존 GO 있으면 파괴 예약
        if (UnitRoot != null) Destroy(UnitRoot);
        if (Player != null) Destroy(Player);

        // 신규 생성
        Player = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        // ★ 활성 씬 루트로 강제 이동시켜 Hierarchy 시인성/일관성 보장
        SceneManager.MoveGameObjectToScene(Player, SceneManager.GetActiveScene());
        Debug.Log($"[PM] Instantiate Player='{Player.name}'");

        var unitRootTr = FindUnitRootTransform(Player.transform);
        if (unitRootTr == null)
        {
            unitRootTr = Player.transform;
            Debug.LogWarning("[PM] 'UnitRoot' not found. Use Player root (legacy).");
        }
        UnitRoot = unitRootTr.gameObject;

        // 위치/속도 초기화
        MoveUnitRoot(unitRootTr, spawnPos, resetVelocity: true);

        // Revive & 완료 플래그
        var pc = UnitRoot.GetComponent<PlayerController>() ?? Player?.GetComponent<PlayerController>();
        if (pc != null) pc.Revive();

        _spawned = true;
        _spawnRunId++;

        // 스폰 직후 잠깐 ResetState 오입력 방지
        _resetLock = true;
        _resetLockUntil = Time.realtimeSinceStartup + 0.5f;

        // 이번 스폰이 신규 스폰이라면 플래그 해제 + 씬 잠금
        if (_forceFreshSpawn)
        {
            _forceFreshSpawn = false;
            _freshSpawnDoneThisScene = true;
            Debug.Log($"[PM] Fresh spawn completed. runId={_spawnRunId}");
        }

        Debug.Log($"[PM] Spawn complete. UnitRoot at {spawnPos} (runId={_spawnRunId})");
        RaiseEquipmentReadyIfPossible();
    }

    private IEnumerator Co_WaitAndSpawnFallback()
    {
        // RoomManager가 늦게 뜨는 경우 대비, 한 프레임 이상 기다렸다가 기본 위치로라도 스폰
        yield return null;
        if (!_spawned)
            PlayerInit(Vector2.zero);
    }

    private IEnumerator Co_FallbackIfNoStartPoint(float waitSeconds)
    {
        float t = 0f;
        while (t < waitSeconds && !_spawned)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!_spawned) PlayerInit(Vector2.zero);
    }

    private void TrySubscribeRoomEvent(RoomManager rm)
    {
        TryUnsubscribeRoomEvent();
        if (rm != null) rm.OnSetStartPoint += HandleStartPoint;
    }

    private void TryUnsubscribeRoomEvent()
    {
        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (rm != null) rm.OnSetStartPoint -= HandleStartPoint;
    }

    private void HandleStartPoint(Vector2 pos)
    {
        // 이미 이번 씬에서 신규 스폰을 끝냈으면 무시
        if (_freshSpawnDoneThisScene)
        {
            Debug.Log("[PM] HandleStartPoint ignored: fresh spawn already done in this scene.");
            return;
        }
        // 이미 스폰 완료 & 신규 모드 아님 → 무시
        if (_spawned && !_forceFreshSpawn)
        {
            Debug.Log("[PM] HandleStartPoint ignored: already spawned.");
            return;
        }
        PlayerInit(pos);
    }

    private void MoveUnitRoot(Transform unitRoot, Vector2 spawnPos, bool resetVelocity)
    {
        var worldPos = (Vector3)spawnPos;
        var rb = unitRoot.GetComponent<Rigidbody2D>();

#if UNITY_6000_0_OR_NEWER
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = worldPos; // Rigidbody2D는 position으로 이동
        }
        else
        {
            unitRoot.position = worldPos;     // 일반 Transform
        }

        if (resetVelocity && rb == null)
        {
            // Rigidbody가 없으면 자식 중 찾아서라도 초기화(필요 시)
            var childRb = unitRoot.GetComponentInChildren<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.linearVelocity = Vector2.zero;
#else
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = worldPos; // Rigidbody2D는 position으로 이동
        }
        else
        {
            unitRoot.position = worldPos;     // 일반 Transform
        }

        if (resetVelocity && rb == null)
        {
            // Rigidbody가 없으면 자식 중 찾아서라도 초기화(필요 시)
            var childRb = unitRoot.GetComponentInChildren<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.velocity = Vector2.zero;
#endif
                childRb.angularVelocity = 0f;
                childRb.position = worldPos;
            }
        }
    }

    /// <summary>
    /// 유연한 UnitRoot 탐색:
    /// 1) 이름 "UnitRoot" 우선
    /// 2) 바로 아래 자식들 중 Rigidbody2D를 가진 Transform
    /// 3) 전체 하위 중 첫 Rigidbody2D 보유 Transform
    /// </summary>
    private Transform FindUnitRootTransform(Transform root)
    {
        if (root == null) return null;

        // 1) 이름 "UnitRoot" 우선
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            if (ch.name == "UnitRoot") return ch;
        }

        // 2) 바로 아래 자식들 중 Rigidbody2D를 가진 Transform
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            if (ch.GetComponent<Rigidbody2D>() != null) return ch;
        }

        // 3) 전체 하위 중 첫 Rigidbody2D 보유 Transform
        var rb = root.GetComponentInChildren<Rigidbody2D>();
        return rb != null ? rb.transform : null;
    }

    private void RaiseEquipmentReadyIfPossible()
    {
        // 장비 매니저는 보통 UnitRoot 하위에 존재
        EquipmentManager eq = null;
        if (UnitRoot != null)
            eq = UnitRoot.GetComponentInChildren<EquipmentManager>(true);
        else if (Player != null)
            eq = Player.GetComponentInChildren<EquipmentManager>(true);

        if (eq != null)
            OnEquipmentReady?.Invoke(eq);
    }

    private void SafeStopFallback()
    {
        if (_fallbackCo != null)
        {
            try { StopCoroutine(_fallbackCo); }
            catch { /* no-op: object may be being destroyed */ }
            _fallbackCo = null;
        }
    }
}
