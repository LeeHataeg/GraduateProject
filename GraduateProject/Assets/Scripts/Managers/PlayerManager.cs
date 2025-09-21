using System;
using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject Player;
    public event Action<EquipmentManager> OnEquipmentReady;

    private PlayerPositionController playerPositionController;

    [SerializeField] private string playerPrefabPath = "Prefabs/Player/Player/Player";
    private bool _spawned;

    private void Awake()
    {
        playerPositionController = GetComponent<PlayerPositionController>();
        if (playerPositionController == null)
            playerPositionController = gameObject.AddComponent<PlayerPositionController>();

        if (GameManager.Instance != null && GameManager.Instance.RoomManager != null)
        {
            GameManager.Instance.RoomManager.OnSetStartPoint += PlayerInit;
            Debug.Log("[PlayerManager] Subscribed to RoomManager.OnSetStartPoint in Awake()");
        }
        else
        {
            Debug.LogWarning("[PlayerManager] RoomManager not ready in Awake. Will retry in Start().");
        }
    }

    private void Start()
    {
        var rm = GameManager.Instance?.RoomManager;
        if (rm != null)
        {
            rm.OnSetStartPoint -= PlayerInit; // 중복 구독 방지
            rm.OnSetStartPoint += PlayerInit;
            Debug.Log("[PlayerManager] Subscribed to RoomManager.OnSetStartPoint in Start()");

            if (rm.HasStartPoint)
            {
                Debug.Log("[PlayerManager] RoomManager already has StartPoint. Spawning immediately.");
                PlayerInit(rm.GetStartPoint());
            }
            else
            {
                Debug.Log("[PlayerManager] No StartPoint yet. Will wait 3s then fallback.");
                StartCoroutine(SpawnFallbackAfterTimeout(3f));
            }
        }
        else
        {
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.RoomManager != null)
            GameManager.Instance.RoomManager.OnSetStartPoint -= PlayerInit;
    }

    private IEnumerator WaitAndSubscribe()
    {
        Debug.Log("[PlayerManager] WaitAndSubscribe...");
        float t = 2f;
        while (t > 0f && (GameManager.Instance == null || GameManager.Instance.RoomManager == null))
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        var rm = GameManager.Instance?.RoomManager;
        if (rm == null)
        {
            Debug.LogError("[PlayerManager] Could not find RoomManager in scene.");
            yield break;
        }

        rm.OnSetStartPoint -= PlayerInit;
        rm.OnSetStartPoint += PlayerInit;
        Debug.Log("[PlayerManager] Subscribed to RoomManager.OnSetStartPoint after wait.");

        if (rm.HasStartPoint)
            PlayerInit(rm.GetStartPoint());
        else
            StartCoroutine(SpawnFallbackAfterTimeout(3f));
    }

    private IEnumerator SpawnFallbackAfterTimeout(float timeout)
    {
        float t = timeout;
        var rm = GameManager.Instance?.RoomManager;
        while (t > 0f && rm != null && !rm.HasStartPoint)
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_spawned)
        {
            Vector2 pos = (rm != null && rm.HasStartPoint) ? rm.GetStartPoint() : Vector2.zero;
            Debug.Log($"[PlayerManager] Fallback spawn at {pos} (timeout or late startpoint)");
            PlayerInit(pos);
        }
    }

    public void PlayerInit(Vector2 pos)
    {
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
