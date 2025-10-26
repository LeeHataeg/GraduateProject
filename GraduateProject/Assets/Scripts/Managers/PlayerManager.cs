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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TryUnsubscribeRoomEvent();
    }

    public void ResetState()
    {
        _spawned = false;
        if (_fallbackCo != null) { StopCoroutine(_fallbackCo); _fallbackCo = null; }
        Player = null;
        UnitRoot = null;
        TryUnsubscribeRoomEvent();
    }

    public void ResetState(bool destroyPlayer = true)
    {
        // 이 매니저에서 돌고 있을 수 있는 코루틴 전부 중단 (있으면 멈추고, 없어도 문제 없음)
        StopAllCoroutines();

        // 현재 플레이어/루트 제거(있으면)
        if (destroyPlayer)
        {
            if (UnitRoot != null) Destroy(UnitRoot);
            if (Player != null) Destroy(Player);
        }

        // 레퍼런스 및 플래그 초기화
        Player = null;
        UnitRoot = null;

        // 다음 회차에서 PlayerInit이 새로 실행되도록 플래그 리셋
        _spawned = false;

        Debug.Log("[PlayerManager] ResetState: cleared current player. Will respawn in next gameplay scene.");
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene)) return;

        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (rm == null)
        {
            if (_fallbackCo != null) StopCoroutine(_fallbackCo);
            _fallbackCo = StartCoroutine(Co_WaitAndSpawnFallback());
            return;
        }

        if (rm.HasStartPoint)
        {
            PlayerInit(rm.GetStartPoint());
        }
        else
        {
            TrySubscribeRoomEvent(rm);
            if (_fallbackCo != null) StopCoroutine(_fallbackCo);
            _fallbackCo = StartCoroutine(Co_FallbackIfNoStartPoint(0.2f));
        }
    }

    private IEnumerator Co_WaitAndSpawnFallback()
    {
        yield return null; // 1프레임 대기
        yield return null; // 2프레임 대기

        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (rm != null)
        {
            if (rm.HasStartPoint) PlayerInit(rm.GetStartPoint());
            else
            {
                TrySubscribeRoomEvent(rm);
                _fallbackCo = StartCoroutine(Co_FallbackIfNoStartPoint(0.2f));
            }
            yield break;
        }

        // 정말 예외적 상황: 0,0 폴백
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
        PlayerInit(pos);
    }

    private bool IsGameplayScene(Scene s)
    {
        if (s.name == gameplaySceneName) return true;
        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        return rm != null && rm.gameObject.scene == s;
    }

    // === 핵심: 실제 스폰/이동은 "UnitRoot"만 조작 ===
    public void PlayerInit(Vector2 spawnPos)
    {
        // 이미 한 번 스폰된 상태라면, RectTransform(Player) 건드리지 말고 UnitRoot만 이동
        if (_spawned && UnitRoot != null)
        {
            MoveUnitRoot(UnitRoot.transform, spawnPos, resetVelocity: true);
#if UNITY_EDITOR
            Debug.Log($"[PlayerManager] Repositioned UnitRoot -> {spawnPos}");
#endif
            RaiseEquipmentReadyIfPossible();
            return;
        }

        // 최초 스폰: 프리팹 준비
        var prefab = playerPrefab != null ? playerPrefab : Resources.Load<GameObject>(playerResourcesPath);
        if (prefab == null)
        {
            Debug.LogError($"[PlayerManager] Player prefab missing. Checked field and Resources({playerResourcesPath})");
            return;
        }

        // 최상위 Player(=RectTransform일 수 있음)는 원래 자리 유지
        // 스폰 좌표는 아래에서 UnitRoot만 이동시키며 적용
        Player = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // UnitRoot 탐색(이름/컴포넌트 기준 모두 지원)
        var unitRootTr = FindUnitRootTransform(Player.transform);
        if (unitRootTr == null)
        {
            // 못 찾으면 최후: Player 자체를 UnitRoot로 취급(레거시 호환)
            unitRootTr = Player.transform;
            Debug.LogWarning("[PlayerManager] 'UnitRoot' child not found. Using Player transform as UnitRoot (legacy fallback).");
        }
        UnitRoot = unitRootTr.gameObject;

        // ★ 스폰 좌표 적용은 UnitRoot에만
        MoveUnitRoot(unitRootTr, spawnPos, resetVelocity: true);

        _spawned = true;

#if UNITY_EDITOR
        Debug.Log($"[PlayerManager] Spawned. UnitRoot at {spawnPos} (Player root left untouched)");
#endif

        RaiseEquipmentReadyIfPossible();
    }

    /// <summary>
    /// UnitRoot만 이동시키고, Rigidbody2D가 있으면 물리 속도도 초기화.
    /// Player(최상위 RectTransform)는 건드리지 않는다.
    /// </summary>
    private void MoveUnitRoot(Transform unitRoot, Vector2 worldPos, bool resetVelocity)
    {
        if (unitRoot == null) return;

        var rb = unitRoot.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
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
#if UNITY_6000_0_OR_NEWER
                childRb.linearVelocity = Vector2.zero;
#else
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
    private Transform FindUnitRootTransform(Transform playerRoot)
    {
        // 1) 이름 기준
        var named = playerRoot.Find("UnitRoot");
        if (named != null) return named;

        // 2) 1-depth에서 Rigidbody2D 가진 자식
        for (int i = 0; i < playerRoot.childCount; i++)
        {
            var ch = playerRoot.GetChild(i);
            if (ch.GetComponent<Rigidbody2D>() != null) return ch;
        }

        // 3) 전체 하위 탐색
        var all = playerRoot.GetComponentsInChildren<Rigidbody2D>(true);
        if (all != null && all.Length > 0) return all[0].transform;

        return null;
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
}
