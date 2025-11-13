using System;
using System.Collections;
using System.Threading;
using UnityEngine;

// 플레이어 생성, 제거, respawn, teleport, spawn 등 담당
[DefaultExecutionOrder(-200)]   // DefaultExecutionOrder~~~ 이건 MonoBehaviour클래스 실행 순서 지정, 값이 작을 수록 먼저 실행
public class PlayerManager : MonoBehaviour
{
    [Header("Refs")]
    public GameObject UnitRoot; // Player 몸체

    // 각 Mono 어쩌고 별 Start, Awake 완료 시간 차이로 인해
    // GetComponent 등에서 오류 발생 가능
    //      따라서  컴포넌트 세팅 완료 여부를 Event 구독으로 통보
    public event Action<EquipmentManager> OnEquipmentReady;

    Animator anim;
    Rigidbody2D rigid;
    Collider2D[] colliders;
    PlayerInputController inputController;
    PlayerMovement plMove;
    HealthController hpCont;
    PlayerHitReactor hitReactor;
    PlayerAttackController atkCont;

    Coroutine spawnCoroutine;
    bool finishPreparing = false;

    private void Awake()
    {
        CacheComponents();
    }

    public void PreparePlayerObj()
    {
        if (finishPreparing) return;
        finishPreparing = true;

        var allPlayers = FindAllPlayerControllers();
        if (allPlayers != null && allPlayers.Length > 0)
        {
            var player = GetRealPlayer(allPlayers);
            var unitRoot = GetUnitRoot(player);
            Adopt(unitRoot);

            foreach (var pl in allPlayers)
            {
                if (!pl) continue;
                var otherUnitRoot = GetUnitRoot(pl);
                if (!otherUnitRoot) continue;
                if (otherUnitRoot == UnitRoot) continue;

                TryDisableInput(otherUnitRoot);                     // 짜가 player 입력 비활성화
                Destroy(otherUnitRoot.transform.root.gameObject);   // 그리고 짜가 obj 제거
            }
        }
        else
        {
            SpawnFromPrefab();
        }

        CacheComponents();
        EnableCombat(true);
        StartCoroutine(Co_BroadcastEquipLater());

        finishPreparing = false;
    }

    private IEnumerator Co_BroadcastEquipLater()
    {
        yield return null;
        BroadcastEquipReady();
    }

    private PlayerController[] FindAllPlayerControllers()
    {
#if UNITY_6000_0_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<PlayerController>(true);
#endif
    }

    private GameObject GetUnitRoot(PlayerController pc)
    {
        if (!pc) return null;
        var sceneRoot = pc.transform.root; // 최상위(보통 'Player')
        var unit = FindChildRecursive(sceneRoot, "UnitRoot");
        return unit ? unit.gameObject : sceneRoot.gameObject;
    }

    private PlayerController GetRealPlayer(PlayerController[] list)
    {
        foreach (var c in list)
        {
            if (!c) continue;
            var root = c.transform.root;
            if (root && (root.name == "Player" || root.name == "UnitRoot"))
                return c;
        }
        foreach (var c in list)
        {
            if (!c) continue;
            if (c.CompareTag("Player") || (c.transform.root && c.transform.root.CompareTag("Player")))
                return c;
        }
        return list[0];
    }

    private void Adopt(GameObject unitRootGO)
    {
        if (!unitRootGO)
        {
            Debug.LogError("[PlayerManager] Adopt: UnitRoot candidate is null.");
            return;
        }

        var unit = (unitRootGO.name == "UnitRoot")
            ? unitRootGO.transform
            : FindChildRecursive(unitRootGO.transform, "UnitRoot");

        UnitRoot = unit ? unit.gameObject : unitRootGO;

        CacheComponents();
        EnableCombat(true); // 안전망
    }

    private void SpawnFromPrefab()
    {
        var prefabRoot = Resources.Load<GameObject>(Const.Prefabs_Player); // "Prefabs/Player/Player/Player"
        if (prefabRoot == null)
        {
            Debug.LogError($"[PlayerManager] Player prefab not found at Resources path: {Const.Prefabs_Player}");
            return;
        }

        var rootGO = Instantiate(prefabRoot);
        rootGO.name = "Player";
        if (!rootGO.CompareTag("Player")) rootGO.tag = "Player";

        var unit = FindChildRecursive(rootGO.transform, "UnitRoot");
        UnitRoot = unit ? unit.gameObject : rootGO;

        CacheComponents();
        EnableCombat(true);
    }

    // 만약에 혹~시라도 플레이어 obj가 여러개일 경우엔
    // 입력 비활성화
    private void TryDisableInput(GameObject unitRootGO)
    {
        if (!unitRootGO) return;
        var input = unitRootGO.GetComponent<PlayerInputController>();
        if (input) input.enabled = false;
        var move = unitRootGO.GetComponent<PlayerMovement>();
        if (move) move.enabled = false;
        var rb = unitRootGO.GetComponent<Rigidbody2D>();
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.simulated = false;
        }
    }

    private void BroadcastEquipReady()
    {
        if (!UnitRoot) return;
        var eq = UnitRoot.GetComponentInChildren<EquipmentManager>(true);
        if (eq != null) OnEquipmentReady?.Invoke(eq);
    }

    // 부활이랑 스폰
    public void Revive()
    {
        if (!UnitRoot) return;
        if (anim == null || rigid == null || colliders == null || atkCont == null) CacheComponents();

        // 1. HP/죽음 플래그 복구
        if (hpCont != null) hpCont.ResetHpToMax();
        if (hitReactor != null) hitReactor.Revive();

        // 2. 애니 복구
        if (anim != null)
        {
            anim.ResetTrigger("4_Death");
            anim.SetBool("isDeath", false);
            TryPlayIfExists(anim, "IDLE");
            TryPlayIfExists(anim, "Idle");
            TryPlayIfExists(anim, "Base Layer.IDLE");
        }

        // 3. 전투 요소 활성화
        EnableCombat(true);

        // 4. 플레이어 컨트롤러 쪽도 복구 호출(구독/상태 싱크)
        var pc = UnitRoot.GetComponent<PlayerController>();
        if (pc != null) pc.Revive();

        // 5. 장비 UI 갱신
        BroadcastEquipReady();
    }
    
    public void SpawnToStartPoint()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(Co_SpawnPlayer());
    }
    private IEnumerator Co_SpawnPlayer()
    {
        // 맵이랑 방이 생성 완료될 때까지 대기
        float timeout = 0.3f;
        while (timeout > 0f)
        {
            if (this == null) yield break; // 파괴되었으면 종료

            var rm = GameManager.Instance?.RoomManager;
            // RoomManager랑 UnitRoot가 준비 여부 +  스폰 포인트가 설정여부 확인
            if (rm != null && UnitRoot != null && rm.HasStartPoint)
                break;

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (this != null && UnitRoot != null)
        {
            var rm = GameManager.Instance?.RoomManager;
            if (rm != null && rm.HasStartPoint)
                rm.TeleportToSpawnPoint(UnitRoot.transform);
        }

        spawnCoroutine = null;
    }

    public void TeleportTo(Vector3 worldPos)
    {
        if (!UnitRoot) return;
        if (rigid != null)
        {
#if UNITY_6000_0_OR_NEWER
            rigid.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
        UnitRoot.transform.position = worldPos;
    }

    public GameObject Player => UnitRoot;

    // 전투 관련
    private void EnableCombat(bool on)
    {
        if (!UnitRoot) return;

        if (inputController) inputController.enabled = on;
        if (plMove) plMove.enabled = on;
        if (atkCont) atkCont.enabled = on;

        if (hitReactor)
        {
            var bhv = hitReactor as Behaviour;
            if (bhv) bhv.enabled = on;
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
                if (c) c.enabled = on;
        }

        if (rigid)
        {
            rigid.simulated = on;
            if (!on)
            {
#if UNITY_6000_0_OR_NEWER
                rigid.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
            }
        }

        int hurtLayer = SafeLayer("PlayerHurtbox");
        var t = UnitRoot.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.name.IndexOf("Hurtbox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.CompareTag("PlayerHurtbox"))
            {
                if (hurtLayer != -1) child.gameObject.layer = hurtLayer;
                foreach (var col in child.GetComponentsInChildren<Collider2D>(true))
                    col.enabled = on;
                foreach (var beh in child.GetComponentsInChildren<Behaviour>(true))
                {
                    if (beh.GetType().Name.Contains("Hurt", StringComparison.OrdinalIgnoreCase) ||
                        beh.GetType().Name.Contains("Hit", StringComparison.OrdinalIgnoreCase))
                        beh.enabled = on;
                }
            }
        }
    }

    private static int SafeLayer(string name)
    {
        int id = LayerMask.NameToLayer(name);
        return (id >= 0 && id < 32) ? id : -1;
    }

    // 필드 캐싱 ㄱㄱ여
    private void CacheComponents()
    {
        if (!UnitRoot) return;
        anim = UnitRoot.GetComponentInChildren<Animator>(true);
        rigid = UnitRoot.GetComponent<Rigidbody2D>();
        colliders = UnitRoot.GetComponentsInChildren<Collider2D>(true);
        inputController = UnitRoot.GetComponent<PlayerInputController>();
        plMove = UnitRoot.GetComponent<PlayerMovement>();
        hpCont = UnitRoot.GetComponent<HealthController>();
        hitReactor = UnitRoot.GetComponent<PlayerHitReactor>();
        atkCont = UnitRoot.GetComponent<PlayerAttackController>();
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (!root) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            var r = FindChildRecursive(c, name);
            if (r != null) return r;
        }
        return null;
    }

    private static void TryPlayIfExists(Animator a, string stateName)
    {
        if (!a) return;
        try { a.Play(stateName, 0, 0f); } catch { }
    }
}
