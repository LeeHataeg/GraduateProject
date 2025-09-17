using System.Collections;
using UnityEngine;

/// <summary>
/// 공통 적 컨트롤러
/// - 탐지 반경 내 추격
/// - 근접: X<=사거리 && Y정렬(허용치)일 때 공격
/// - 원거리(OrcArcher): Fire/AtkPoint 기준 Y±1f일 때만 사격 (X는 탐지 반경 내면 OK)
///   다른 원거리 유형은 Y 창 무시(원한다면 인스펙터 옵션으로 켜기)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ---- 외부 컴포넌트
    private ICombatStatHolder combatStatHolder;
    private IAnimationController anim;
    private IHitReactor hitReactor;
    private IHealth healthCtrl;

    // 공격기(둘 다 지원)
    private IAttackBehavior attackBehavior;              // RangedAttackBehavior는 인터페이스 구현
    private RangedAttackBehavior ranged;                 // 명시 타입
    private MeleeAttackBehavior melee;                   // 인터페이스 미구현 대응

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;

    [Header("Component Root / Points")]
    [Tooltip("비워두면 'UnitRoot' 또는 Stat/RB 자식을 자동 탐색")]
    [SerializeField] private Transform componentRoot;
    [Tooltip("공격 시작 위치. 비워두면 FirePoint/AtkPoint/AttackPoint를 자동 탐색")]
    [SerializeField] private Transform atkStartPoint;

    [Header("AI Tuning")]
    [SerializeField] private float stopDistance = 0.05f;    // 멈춤 임계 X거리
    [SerializeField] private float yThresholdMelee = 0.7f;  // 근접 Y허용치
    [SerializeField] private float attackCooldown = 0.8f;   // 공용 쿨
    [SerializeField] private float windup = 0.08f;          // 선딜
    [SerializeField] private float recover = 0.12f;         // 후딜

    [Header("Ranged Height Gate")]
    [Tooltip("원거리 공격에서 Y정렬을 요구할지(기본: Archer만 자동 적용)")]
    [SerializeField] private bool useRangedYGate = false;
    [Tooltip("원거리 Y정렬 허용치(요구사항: OrcArcher는 1.0f)")]
    [SerializeField] private float rangedYThreshold = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool isDead = false;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        if (!componentRoot) componentRoot = ResolveComponentRoot();
        AcquireComponents(strict: false);

        // Archer 자동 감지로 Y Gate 켜주기(이름기반, 필요시 인스펙터로 수동 덮어쓰기 가능)
        if (ranged && !useRangedYGate)
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("archer") || n.Contains("bow")) useRangedYGate = true;
        }

        // 공격 시작점 자동
        if (!atkStartPoint)
            atkStartPoint = FindFirstAttackStartPoint();
    }

    private void OnEnable()
    {
        TryFindPlayer();
    }

    private void Start()
    {
        if (healthCtrl != null)
            healthCtrl.OnDead += HandleDeath;
    }

    private void Update()
    {
        if (isDead || player == null || combatStatHolder?.Stats == null) return;

        float distToPlayer = Vector2.Distance(CurrentPos, player.position);
        float detect = Mathf.Max(0.01f, combatStatHolder.Stats.DetectionRadius);

        if (distToPlayer <= detect)
        {
            ChaseAndFace();
            TryAttack(distToPlayer, detect);
        }
        else
        {
            anim?.SetBool("1_Move", false);
            HaltX();
        }
    }

    // ----------------- 공격 로직 -----------------

    private void TryAttack(float distToPlayer, float detectionRadius)
    {
        if (isDead || isAttacking) return;
        if (Time.time < nextAttackTime) return;

        // 공격기 확인(둘 중 하나라도 있어야 함)
        if (!ranged && !melee && attackBehavior == null)
        {
            AcquireComponents(strict: false);
            if (!ranged && !melee && attackBehavior == null) return;
        }

        // 시작점 Y
        float yRef = atkStartPoint ? atkStartPoint.position.y : CurrentPos.y;
        float deltaY = Mathf.Abs(player.position.y - yRef);

        // 범용 사거리(최대값 사용)
        float behaviorRange = 0f;
        if (attackBehavior != null) behaviorRange = Mathf.Max(behaviorRange, attackBehavior.Range);
        if (ranged != null) behaviorRange = Mathf.Max(behaviorRange, ranged.Range);
        if (melee != null) behaviorRange = Mathf.Max(behaviorRange, melee.Range);
        float statRange = combatStatHolder?.Stats != null ? Mathf.Max(0f, combatStatHolder.Stats.AttackRange) : 0f;
        float useRange = Mathf.Max(behaviorRange, statRange);

        bool canAttack = false;
        Vector2 startPos = atkStartPoint ? (Vector2)atkStartPoint.position : CurrentPos;

        if (ranged != null) // 원거리
        {
            // 요구사항: OrcArcher는 Fire/AtkPoint 기준 Y±1에서만 발사
            bool yOk = !useRangedYGate || (deltaY <= rangedYThreshold);

            // 원거리는 X거리 대신 "탐지 반경 내"를 만족하면 충분(라인슈트/상대적 사거리 대신)
            if (yOk && distToPlayer <= detectionRadius)
                canAttack = true;
        }
        else if (melee != null) // 근접
        {
            float dx = Mathf.Abs(player.position.x - CurrentPos.x);
            bool yOk = deltaY <= yThresholdMelee;
            if (yOk && dx <= useRange)
                canAttack = true;
        }
        else if (attackBehavior != null) // 인터페이스만 있는 특수형
        {
            // 기본값: 근접과 동일한 판정
            float dx = Mathf.Abs(player.position.x - CurrentPos.x);
            bool yOk = deltaY <= yThresholdMelee;
            if (yOk && dx <= useRange)
                canAttack = true;
        }

        if (!canAttack) return;

        StartCoroutine(AttackRoutine(startPos, useRange));
    }

    private IEnumerator AttackRoutine(Vector2 startPos, float useRange)
    {
        isAttacking = true;
        try
        {
            anim?.SetTrigger("2_Attack");
            HaltX();

            if (windup > 0f) yield return new WaitForSeconds(windup);

            float dmg = combatStatHolder != null ? combatStatHolder.CalculatePhysicsDmg() : 1f;
            if (TryGetComponent<DamageMultiplier>(out var mul)) dmg = mul.Apply(dmg);

            // 실행(우선순위: 명시 클래스 → 인터페이스)
            if (ranged != null)
            {
                ranged.Execute(startPos, dmg, useRange);
            }
            else if (melee != null)
            {
                melee.Execute(startPos, dmg, useRange);
            }
            else if (attackBehavior != null)
            {
                attackBehavior.Execute(startPos, dmg, useRange);
            }

            if (recover > 0f) yield return new WaitForSeconds(recover);
        }
        finally
        {
            isAttacking = false;
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
            if (debugLog) Debug.Log($"[EnemyController] Attack done. next@{nextAttackTime:0.00} ({name})");
        }
    }

    // ----------------- 이동/시선 -----------------

    private void ChaseAndFace()
    {
        if (isDead || player == null || combatStatHolder?.Stats == null) return;

        float dx = player.position.x - CurrentPos.x;
        float dirX = dx > 0.02f ? 1f : (dx < -0.02f ? -1f : 0f);
        float moveSpeed = Mathf.Max(0f, combatStatHolder.Stats.MoveSpeed);

        // 시선: 오른쪽=scale.x>0 기준(왼쪽보는 스프라이트면 필요에 맞게 뒤집힘)
        if (dirX != 0f)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dirX > 0 ? -1f : 1f);
            transform.localScale = s;
        }

        if (isAttacking)
        {
            anim?.SetBool("1_Move", false);
            HaltX();
            return;
        }

        // 원거리도 기본은 추격(필요 시 몬스터별로 변경)
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity;
        v.x = dirX * moveSpeed;
        rb.linearVelocity = v;
#else
        var v = rb.velocity;
        v.x = dirX * moveSpeed;
        rb.velocity = v;
#endif
        anim?.SetBool("1_Move", Mathf.Abs(dirX) > 0f);
    }

    private void HaltX()
    {
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = 0f; rb.linearVelocity = v;
#else
        var v = rb.velocity; v.x = 0f; rb.velocity = v;
#endif
    }

    // ----------------- 컴포넌트/유틸 -----------------

    private Vector2 CurrentPos => componentRoot ? (Vector2)componentRoot.position : (Vector2)transform.position;

    private void AcquireComponents(bool strict)
    {
        Transform root = componentRoot ? componentRoot : transform;

        // 스탯/애니메/체력/히트
        combatStatHolder = root.GetComponent<ICombatStatHolder>() ?? GetComponentInChildren<ICombatStatHolder>(true);
        anim = root.GetComponent<IAnimationController>() ?? GetComponentInChildren<IAnimationController>(true);
        hitReactor = root.GetComponent<IHitReactor>() ?? GetComponentInChildren<IHitReactor>(true);
        healthCtrl = root.GetComponent<IHealth>() ?? GetComponentInChildren<IHealth>(true);

        // 공격기(둘 다 시도)
        attackBehavior = root.GetComponent<IAttackBehavior>() ?? GetComponentInChildren<IAttackBehavior>(true);
        ranged = root.GetComponent<RangedAttackBehavior>() ?? GetComponentInChildren<RangedAttackBehavior>(true);
        melee = root.GetComponent<MeleeAttackBehavior>() ?? GetComponentInChildren<MeleeAttackBehavior>(true);

        if (strict)
        {
            if (combatStatHolder == null) Debug.LogWarning($"[EnemyController] No ICombatStatHolder on {name}");
            if (anim == null) Debug.LogWarning($"[EnemyController] No IAnimationController on {name}");
            if (healthCtrl == null) Debug.LogWarning($"[EnemyController] No IHealth on {name}");
            if (ranged == null && melee == null && attackBehavior == null)
                Debug.LogWarning($"[EnemyController] No AttackBehavior on {name}");
        }
    }

    private Transform ResolveComponentRoot()
    {
        // 1) 이름 'UnitRoot'
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.Equals("UnitRoot", System.StringComparison.OrdinalIgnoreCase))
                return t;

        // 2) 스탯 or 물리 콤보가 있는 자식
        foreach (var m in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!m) continue;
            if (m is ICombatStatHolder || m.GetComponent<StatController>() ||
                (m.GetComponent<Rigidbody2D>() && m.GetComponent<Collider2D>()))
                return m.transform;
        }
        return transform;
    }

    private Transform FindFirstAttackStartPoint()
    {
        // Ranged 우선 FirePoint
        var fp = FindChildByNameCI(componentRoot ? componentRoot : transform, "FirePoint");
        if (fp) return fp;

        // Melee AtkPoint/AttackPoint
        var ap = FindChildByNameCI(componentRoot ? componentRoot : transform, "AtkPoint");
        if (ap) return ap;
        ap = FindChildByNameCI(componentRoot ? componentRoot : transform, "AttackPoint");
        if (ap) return ap;

        // MeleeAttackBehavior의 AttackPoint 필드 활용(있다면)
        if (melee && melee.AttackPoint) return melee.AttackPoint;

        return componentRoot ? componentRoot : transform;
    }

    private static Transform FindChildByNameCI(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    private void TryFindPlayer()
    {
        GameObject p = null;
        try { p = GameObject.FindGameObjectWithTag("Player"); } catch { }
        player = p ? p.transform : null;
    }

    // ----------------- 사망 -----------------

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        anim?.SetTrigger("4_Death");
        anim?.SetBool("isDeath", true);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
        if (col) col.enabled = false;

        Destroy(gameObject, 1.5f);
    }
}
