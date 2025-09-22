using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    private bool _spawned;
    public GameObject Player;
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
    private void SetupForScene(Scene scene)
    {
        if (IsGameplayScene(scene))
        {
            // 인게임 진입: 룸 이벤트 구독 보장 + 폴백 준비
            TrySubscribeRoomEventOnce();

            var rm = GameManager.Instance?.RoomManager;
            if (rm != null && rm.HasStartPoint)
            {
                // 이미 StartPoint가 있으면 즉시 스폰
                PlayerInit(rm.GetStartPoint());
            }
            else
            {
                // 폴백 코루틴이 없으면 시작
                if (fallbackCo == null)
                    fallbackCo = StartCoroutine(SpawnFallbackAfterTimeout(3f));
            }
        }
        else
        {
            // 비-게임플레이 씬: 남아있는 것들 정리
            if (fallbackCo != null) { StopCoroutine(fallbackCo); fallbackCo = null; }
            TryUnsubscribeRoomEvent();

            if (Player != null)
            {
                Destroy(Player);
                Player = null;
                _spawned = false;
            }
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

        // Target 연결 후 위치 적용
        playerPositionController.SetTarget(Player.transform);
        ApplyPosition(pos);

        // Unit Root 찾기
        var unitRoot = Player.transform.Find("Unit Root");
        if (unitRoot == null)
            unitRoot = Player.GetComponentInChildren<PlayerMovement>(true)?.transform;

        if (unitRoot == null)
        {
            Debug.LogError("[PlayerManager] Unit Root NOT FOUND. Does your prefab have 'Unit Root' child or PlayerMovement on a child?");
            return;
        }

        var eq = unitRoot.GetComponent<EquipmentManager>();
        var stat = unitRoot.GetComponent<StatController>(); // 또는 PlayerStatController

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
            Player.transform.position = pos;
    }

    // === 인스펙터에서 바로 테스트할 수 있는 강제 스폰 기능 ===
    [ContextMenu("DEBUG: Force Spawn @ (0,0)")]
    private void DebugForceSpawn()
    {
        Debug.Log("[PlayerManager] DEBUG force spawn @ (0,0)");
        PlayerInit(Vector2.zero);
    }
}
