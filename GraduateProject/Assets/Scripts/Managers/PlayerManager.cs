using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    private bool _spawned;
    public GameObject Player;
    public GameObject UnitRoot;
    private PlayerPositionController playerPositionController;
    public event Action<EquipmentManager> OnEquipmentReady;

    [SerializeField] private string playerPrefabPath = "Prefabs/Player/Player/Player";

    [Header("Scene Guard")]
    [SerializeField] private string gameplaySceneName = "InGameScene"; // 인스펙터에서 변경 가능

    private bool subscribed;           // RoomManager 이벤트 중복 구독 방지
    private Coroutine fallbackCo;      // 폴백 스폰 코루틴 핸들

    //=== Lifecycle ===//
    private void Awake()
    {
        playerPositionController = GetComponent<PlayerPositionController>();
        if (playerPositionController == null)
            playerPositionController = gameObject.AddComponent<PlayerPositionController>();

        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 활성 씬에 맞춰 즉시 초기화(씬 로드시만 OnSceneLoaded가 호출되므로, 첫 진입 씬에서도 동작 보장)
        SetupForActiveScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TryUnsubscribeRoomEvent();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ Additive로 로드된 BossField 같은 보조 씬은 건드리지 말자
        if (mode == LoadSceneMode.Additive && scene.name != gameplaySceneName)
            return;

        SetupForScene(scene);
    }


    //=== Scene Helpers ===//
    private bool IsGameplayScene(Scene s) => s.name == gameplaySceneName;
    private bool IsGameplayScene() => SceneManager.GetActiveScene().name == gameplaySceneName;

    /// <summary>현재 활성 씬 기준으로 세팅.</summary>
    private void SetupForActiveScene()
    {
        SetupForScene(SceneManager.GetActiveScene());
    }

    /// <summary>지정된 씬 기준으로 세팅.</summary>
    private bool AnyGameplaySceneLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name == gameplaySceneName) return true;
        }
        return false;
    }

    private void SetupForScene(Scene scene)
    {
        if (IsGameplayScene(scene))
        {
            // (기존 그대로)
            TrySubscribeRoomEventOnce();
            var rm = GameManager.Instance?.RoomManager;
            if (rm != null && rm.HasStartPoint)
                PlayerInit(rm.GetStartPoint());
            else if (fallbackCo == null)
                fallbackCo = StartCoroutine(SpawnFallbackAfterTimeout(3f));
        }
        else
        {
            // ✅ 여기서 무작정 Player를 Destroy 하던 로직을 '진짜로 게임플레이 씬이 하나도 없을 때만' 실행
            if (fallbackCo != null) { StopCoroutine(fallbackCo); fallbackCo = null; }
            TryUnsubscribeRoomEvent();

            if (!AnyGameplaySceneLoaded())
            {
                if (Player != null)
                {
                    Destroy(Player);
                    Player = null;
                    _spawned = false;
                }
            }
            // ✅ Additive로 보조 씬이 늘어난 경우엔 여기서 아무것도 파괴하지 않음
        }
    }


    private void TrySubscribeRoomEventOnce()
    {
        if (subscribed) return;
        var rm = GameManager.Instance?.RoomManager;
        if (rm == null) return;

        rm.OnSetStartPoint -= PlayerInit; // 방어적 해제
        rm.OnSetStartPoint += PlayerInit;
        subscribed = true;
        Debug.Log("[PlayerManager] Subscribed to RoomManager.OnSetStartPoint");
    }

    private void TryUnsubscribeRoomEvent()
    {
        if (!subscribed) return;
        var rm = GameManager.Instance?.RoomManager;
        if (rm != null) rm.OnSetStartPoint -= PlayerInit;
        subscribed = false;
    }

    //=== Fallback Spawn ===//
    private IEnumerator SpawnFallbackAfterTimeout(float timeout)
    {
        // 인게임 씬에서만 동작
        float t = timeout;
        while (t > 0f)
        {
            if (!IsGameplayScene()) { fallbackCo = null; yield break; }

            var rm = GameManager.Instance?.RoomManager;
            if (rm != null && rm.HasStartPoint)
            {
                // 기다리는 중에 StartPoint가 도착했으면 즉시 스폰하고 종료
                PlayerInit(rm.GetStartPoint());
                fallbackCo = null;
                yield break;
            }

            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsGameplayScene()) { fallbackCo = null; yield break; }

        // 여전히 시작점이 없으면 (0,0) 등 폴백 스폰 시도
        if (!_spawned)
        {
            Debug.Log("[PlayerManager] Fallback spawn (timeout, no startpoint)");
            PlayerInit(Vector2.zero);
        }
        fallbackCo = null;
    }

    //=== Public API ===//
    public void PlayerInit(Vector2 pos)
    {
        // ✋ 인게임 씬에서만 생성/이동 허용
        if (!IsGameplayScene())
        {
            Debug.Log("[PlayerManager] Ignored PlayerInit outside gameplay scene.");
            return;
        }

        Debug.Log($"[PlayerManager] PlayerInit called with pos={pos}  spawned={_spawned}");

        if (_spawned && Player != null)
        {
            ApplyPosition(pos);
            Debug.Log("[PlayerManager] Already spawned → moved to pos.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(playerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PlayerManager] Resources.Load FAILED at path: {playerPrefabPath}\n" +
                           "Check folder/case: Assets/Resources/Prefabs/Player/Player/Player.prefab");
            return;
        }

        Player = Instantiate(prefab);
        _spawned = true;
        Debug.Log($"[PlayerManager] Player instantiated: {Player.name}");

        // Unit Root 찾기
        UnitRoot = Player.transform.Find("UnitRoot").gameObject;
        if (UnitRoot == null)
            UnitRoot = Player.GetComponentInChildren<PlayerMovement>(true)?.transform.gameObject;

        if (UnitRoot == null)
        {
            Debug.LogError("[PlayerManager] Unit Root NOT FOUND. Does your prefab have 'Unit Root' child or PlayerMovement on a child?");
            return;
        }

        // Target 연결 후 위치 적용
        playerPositionController.SetTarget(UnitRoot.transform);
        ApplyPosition(pos);

        var eq = UnitRoot.GetComponent<EquipmentManager>();
        var stat = UnitRoot.GetComponent<StatController>(); // 또는 PlayerStatController

        if (eq == null || stat == null)
        {
            Debug.LogError($"[PlayerManager] Required components missing on Unit Root. eq={eq} stat={stat}");
            return;
        }

        OnEquipmentReady?.Invoke(eq);
        Debug.Log($"[PlayerManager] Player ready @ {Player.transform.position}");
    }

    private void ApplyPosition(Vector2 pos)
    {
        if (playerPositionController != null)
            playerPositionController.SetPosition(pos);
        else if (Player != null)
            UnitRoot.transform.position = pos;
    }

    // === 인스펙터에서 바로 테스트할 수 있는 강제 스폰 기능 ===
    [ContextMenu("DEBUG: Force Spawn @ (0,0)")]
    private void DebugForceSpawn()
    {
        Debug.Log("[PlayerManager] DEBUG force spawn @ (0,0)");
        PlayerInit(Vector2.zero);
    }
}
