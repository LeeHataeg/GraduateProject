using UnityEngine;
using static Define;

/// <summary>
/// 적 프리팹 조립기:
/// - 지정된 "컴포넌트 루트"(보통 UnitRoot)에서만 스탯/공격기/버프를 세팅
/// - Start와 Setup의 중복 초기화를 방지
/// </summary>
[DisallowMultipleComponent]
public class EnemyAssembler : MonoBehaviour
{
    [SerializeField] private EnemyArchetypeSO archetype;

    [Header("Component Root (옵션)")]
    [Tooltip("비워두면 자동으로 'UnitRoot' 이름 검색 → 없으면 StatController/ICombatStatHolder가 있는 자식을 사용")]
    [SerializeField] private Transform componentRoot;

    private bool initialized;

    /// <summary>외부(Spawner)가 호출하는 초기화 진입점</summary>
    public void Setup(EnemyArchetypeSO arch, Transform rootOverride = null)
    {
        if (initialized) return;
        archetype = arch;
        componentRoot = rootOverride ? rootOverride : ResolveComponentRoot();
        if (!componentRoot) componentRoot = transform; // 최후 폴백

        ApplyStats();
        ApplyAttackMode();
        ApplyBuffAuraIfAny();

        initialized = true;
    }

    private void Start()
    {
        // 프리팹에 Assembler만 미리 붙여둔 경우를 위한 자동 셋업
        if (!initialized && archetype != null)
            Setup(archetype, componentRoot);
    }

    // ---- 내부 구현 ----

    private Transform ResolveComponentRoot()
    {
        // 1) 이름이 "UnitRoot" 인 자식 우선(대소문자 무시)
        var t = FindChildRecursiveByName(transform, "UnitRoot");
        if (t) return t;

        // 2) StatController / ICombatStatHolder 가 있는 자식
        var holders = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var h in holders)
        {
            if (!h) continue;
            if (h is ICombatStatHolder || h.GetComponent<StatController>())
                return h.transform;
        }

        // 3) 물리 루트 후보(Rigidbody2D+Collider2D) 있는 자식
        foreach (var h in holders)
        {
            if (!h) continue;
            if (h.GetComponent<Rigidbody2D>() && h.GetComponent<Collider2D>())
                return h.transform;
        }

        // 4) 실패 → 자기 자신
        return transform;
    }

    private static Transform FindChildRecursiveByName(Transform root, string nameCI)
    {
        if (!root) return null;
        var targetLower = nameCI.ToLowerInvariant();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLowerInvariant() == targetLower) return t;
        }
        return null;
    }

    private GameObject RootGO => componentRoot ? componentRoot.gameObject : gameObject;

    private void ApplyStats()
    {
        if (!archetype || !archetype.statSheet)
        {
            Debug.LogWarning($"[EnemyAssembler] Missing statSheet on {name}");
            return;
        }

        // 루트에서 StatController/ICombatStatHolder 탐색
        var statHolder = RootGO.GetComponent<ICombatStatHolder>();
        if (statHolder == null)
            statHolder = RootGO.GetComponentInChildren<ICombatStatHolder>(true);

        if (statHolder is StatController sc)
        {
            // StatController에 공개 세터가 없다면 리플렉션으로 주입
            var f = typeof(StatController).GetField("stats",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null) f.SetValue(sc, archetype.statSheet);
        }
        else
        {
            Debug.LogWarning($"[EnemyAssembler] {RootGO.name} has no StatController/ICombatStatHolder.");
        }
    }

    private void ApplyAttackMode()
    {
        // 루트에서 기존 공격기 제거(중복 방지)
        foreach (var c in RootGO.GetComponents<IAttackBehavior>())
        {
            if (c is Component comp) Destroy(comp);
        }

        switch (archetype.attackMode)
        {
            case AttackMode.Melee:
                var x = RootGO.GetComponent<MeleeAttackBehavior>();
                if (!RootGO.GetComponent<MeleeAttackBehavior>())
                    x = RootGO.AddComponent<MeleeAttackBehavior>();

                x.Configure(LayerMask.GetMask("Player"));
                break;

            case AttackMode.Ranged:
            case AttackMode.RangedBuffer:
                {
                    var r = RootGO.GetComponent<RangedAttackBehavior>();
                    if (!r) r = RootGO.AddComponent<RangedAttackBehavior>();
                    r.Configure(
                        archetype.projectilePrefab,
                        archetype.projectileSpeed,
                        archetype.projectileLife,
                        archetype.projectileHitMask,
                        archetype.attackRangeOverride
                    );

                    // FirePoint 자동 연결(있을 때만)
                    var fp = FindChildRecursiveByName(componentRoot ? componentRoot : transform, "FirePoint");
                    if (fp) r.SetFirePoint(fp);
                    break;
                }
        }
    }

    private void ApplyBuffAuraIfAny()
    {
        if (archetype.attackMode != AttackMode.RangedBuffer || !archetype.useBuffAura) return;

        var target = RootGO;
        var aura = target.GetComponent<BuffAura>();
        if (!aura) aura = target.AddComponent<BuffAura>();

        aura.radius = archetype.buffRadius;
        aura.duration = archetype.buffDuration;
        aura.refreshRate = archetype.buffRefreshRate;
        aura.dmgMultiplier = archetype.dmgMultiplier;

        // 샤먼과 같은 레이어만 버프 (자동)
        aura.allyMask = 1 << target.layer;
    }
}
