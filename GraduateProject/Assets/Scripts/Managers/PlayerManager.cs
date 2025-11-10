using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 생성/중복 제거/부활/텔레포트 등 런타임 제어 담당
/// - StartScene에선 생성하지 않음
/// - InGameScene에서만 "씬에 있으면 채택(adopt), 없으면 1회 생성"
/// - 중복 발견 시 입력 차단 후 제거
/// - Player 최상위 루트 아래에 UnitRoot(실행체)를 두는 프로젝트 구조를 전제로 함
/// </summary>
[DefaultExecutionOrder(-200)]
public class PlayerManager : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("실제 조작 대상이 되는 최상위 루트(보통 'Player') 아래의 실행체 'UnitRoot'를 가리키도록 유지")]
    public GameObject UnitRoot;   // ← 'Player' 루트 아래 자식 'UnitRoot' 오브젝트

    public event Action<EquipmentManager> OnEquipmentReady;

    // 캐시
    Animator _anim;
    Rigidbody2D _rb;
    Collider2D[] _cols;
    PlayerInputController _input;
    PlayerMovement _move;
    IAnimationController _animCtrl;
    HealthController _hp;
    PlayerHitReactor _hitReactor;                // 피격 반응 담당(플래그 초기화 필요)
    PlayerAttackController _atk;                 // ★ 공격 컨트롤러

    public string gameplaySceneName = "InGameScene";
    bool _busyPreparing = false;

    private void Awake()
    {
        CacheComponents(); // null일 수 있음
    }

    public void PreparePlayerForScene()
    {
        if (_busyPreparing) return;
        _busyPreparing = true;

        var candidates = FindAllPlayerControllersInScene();
        if (candidates != null && candidates.Length > 0)
        {
            var primary = PickPrimary(candidates);
            var adoptedUnitRoot = GetUnitRootFromAny(primary);
            Adopt(adoptedUnitRoot);

            foreach (var c in candidates)
            {
                if (!c) continue;
                var otherUnitRoot = GetUnitRootFromAny(c);
                if (!otherUnitRoot) continue;
                if (otherUnitRoot == UnitRoot) continue;

                TryDisableInput(otherUnitRoot);
                Destroy(otherUnitRoot.transform.root.gameObject);
            }
        }
        else
        {
            SpawnFromPrefab();
        }

        CacheComponents();
        EnableCombat(true);                   // ★ 전투 요소 확실히 활성화
        StartCoroutine(Co_BroadcastEqNextFrame());

        _busyPreparing = false;
    }

    private IEnumerator Co_BroadcastEqNextFrame()
    {
        yield return null;
        BroadcastEquipmentReadyIfFound();
    }

    private PlayerController[] FindAllPlayerControllersInScene()
    {
#if UNITY_6000_0_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<PlayerController>(true);
#endif
    }

    private GameObject GetUnitRootFromAny(PlayerController pc)
    {
        if (!pc) return null;
        var sceneRoot = pc.transform.root; // 최상위(보통 'Player')
        var unit = FindChildRecursive(sceneRoot, "UnitRoot");
        return unit ? unit.gameObject : sceneRoot.gameObject;
    }

    private PlayerController PickPrimary(PlayerController[] list)
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

    private void BroadcastEquipmentReadyIfFound()
    {
        if (!UnitRoot) return;
        var eq = UnitRoot.GetComponentInChildren<EquipmentManager>(true);
        if (eq != null) OnEquipmentReady?.Invoke(eq);
    }

    // ───── Revive/TP ─────
    public void Revive()
    {
        if (!UnitRoot) return;
        if (_anim == null || _rb == null || _cols == null || _atk == null) CacheComponents();

        // 1) HP/죽음 플래그 복구
        if (_hp != null) _hp.ResetHpToMax();
        if (_hitReactor != null) _hitReactor.ClearDeadFlag();

        // 2) 애니 복구
        if (_anim != null)
        {
            _anim.ResetTrigger("4_Death");
            _anim.SetBool("isDeath", false);
            TryPlayIfExists(_anim, "IDLE");
            TryPlayIfExists(_anim, "Idle");
            TryPlayIfExists(_anim, "Base Layer.IDLE");
        }

        // 3) 전투 요소 활성화
        EnableCombat(true);

        // 4) ★ 플레이어 컨트롤러 쪽도 복구 호출(구독/상태 싱크)
        var pc = UnitRoot.GetComponent<PlayerController>();
        if (pc != null) pc.Revive();

        // 5) 장비 UI 갱신
        BroadcastEquipmentReadyIfFound();
    }

    public void TeleportTo(Vector3 worldPos)
    {
        if (!UnitRoot) return;
        if (_rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
        UnitRoot.transform.position = worldPos;
    }

    public GameObject Player => UnitRoot;

    // ───── Combat Enabler ─────
    private void EnableCombat(bool on)
    {
        if (!UnitRoot) return;

        if (_input) _input.enabled = on;
        if (_move) _move.enabled = on;
        if (_atk) _atk.enabled = on;

        if (_hitReactor)
        {
            var bhv = _hitReactor as Behaviour;
            if (bhv) bhv.enabled = on;
        }

        if (_cols != null)
        {
            foreach (var c in _cols)
                if (c) c.enabled = on;
        }

        if (_rb)
        {
            _rb.simulated = on;
            if (!on)
            {
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
            }
        }

        // (선택) Hurtbox 자식 자동 보정(레이어/콜라이더/스크립트)
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

    // ───── helpers ─────
    private void CacheComponents()
    {
        if (!UnitRoot) return;
        _anim = UnitRoot.GetComponentInChildren<Animator>(true);
        _rb = UnitRoot.GetComponent<Rigidbody2D>();
        _cols = UnitRoot.GetComponentsInChildren<Collider2D>(true);
        _input = UnitRoot.GetComponent<PlayerInputController>();
        _move = UnitRoot.GetComponent<PlayerMovement>();
        _animCtrl = UnitRoot.GetComponent<IAnimationController>();
        _hp = UnitRoot.GetComponent<HealthController>();
        _hitReactor = UnitRoot.GetComponent<PlayerHitReactor>();
        _atk = UnitRoot.GetComponent<PlayerAttackController>();
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
