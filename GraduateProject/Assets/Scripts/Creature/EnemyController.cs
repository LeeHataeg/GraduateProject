using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    private ICombatStatHolder combatStatHolder;
    private IAttackBehavior attackBehavior;
    private IHitReactor hitReactor;
    private IAnimationController anim;
    private IHealth healthCtrl;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;

    [Header("Component Root")]
    [Tooltip("비워두면 자동으로 'UnitRoot' 또는 Stat/RB가 붙은 자식을 찾아 사용")]
    [SerializeField] private Transform componentRoot;

    [Header("Points")]
    [SerializeField] private Transform atkStartPoint;   // 미지정이면 componentRoot.position 사용

    [Header("Facing / Flip")]
    [SerializeField] private SpriteRenderer sprite;      // 없으면 자동 획득
    [Tooltip("스프라이트의 기본 바라보는 방향이 '오른쪽'인지 여부(SPUM 기본이 왼쪽인 경우가 많음)")]
    private bool lookLeft = true;
    private Vector3 flipLeft = new Vector3(1, 1, 1);
    private Vector3 flipRight = new Vector3(-1, 1, 1);

    [Header("AI Tuning")]
    [SerializeField] private float yThreshold = 0.7f;    // 근접 공격 시 수직 오차 허용
    [SerializeField] private float stopDistance = 0.05f; // 멈출 임계 X거리
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float windup = 0.12f;       // 공격 선딜
    [SerializeField] private float recover = 0.18f;      // 후딜
    [Tooltip("원거리 공격은 Y축 정렬 검사를 완화/무시")]
    [SerializeField] private bool ignoreYForRanged = true;

    [Header("Targeting")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float reacquireInterval = 0.75f; // 주기적 타깃/컴포넌트 재탐색
    private float _nextReacquireAt;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool isDead = false;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private Vector3 baseScale;

    private void OnEnable()
    {
        TryFindPlayer(true);
        // 컴포넌트 루트가 비었으면 지금 찾아둔다
        if (!componentRoot) componentRoot = ResolveComponentRoot();
        // 필요한 레퍼런스 바인딩
        AcquireComponents(strict: false);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(true);
        baseScale = transform.localScale;

        if (rb)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        if (col) col.enabled = true;
    }

    private void Start()
    {
        // 혹시 Awake/OnEnable 타이밍에 못 찾았을 경우 한 번 더
        if (!componentRoot) componentRoot = ResolveComponentRoot();
        AcquireComponents(strict: false);

        if (healthCtrl != null)
            healthCtrl.OnDead += HandleDeath;
    }

    private void Update()
    {
        if (isDead) return;

        // 주기적 재탐색(프리팹 구조가 다르거나 조립 순서/지연 대비)
        if (Time.time >= _nextReacquireAt)
        {
            if (!player || !player.gameObject.activeInHierarchy) TryFindPlayer(false);
            if (attackBehavior == null || combatStatHolder?.Stats == null || anim == null || healthCtrl == null)
                AcquireComponents(strict: false);

            _nextReacquireAt = Time.time + reacquireInterval;
        }

        if (player == null || combatStatHolder?.Stats == null) return;

        float distToPlayer = Vector2.Distance(CurrentPos, player.position);

        if (distToPlayer <= combatStatHolder.Stats.DetectionRadius)
        {
            ChaseAndFace();
            TryAttack();
        }
        else
        {
            anim?.SetBool("1_Move", false);
            Halt();
        }
    }

    // ----------------- 탐색/유틸 -----------------

    private Vector2 CurrentPos => componentRoot ? (Vector2)componentRoot.position : (Vector2)transform.position;

    private Transform ResolveComponentRoot()
    {
        // 1) 이름 'UnitRoot'
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.Equals("UnitRoot", System.StringComparison.OrdinalIgnoreCase))
                return t;

        // 2) StatHolder / StatController가 붙은 자식
        var monos = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (!m) continue;
            if (m is ICombatStatHolder || m.GetComponent<StatController>()) return m.transform;
        }

        // 3) Rigidbody2D + Collider2D 조합 자식
        foreach (var m in monos)
        {
            if (!m) continue;
            if (m.GetComponent<Rigidbody2D>() && m.GetComponent<Collider2D>()) return m.transform;
        }

        // 4) 실패 → 자신
        return transform;
    }

    private void AcquireComponents(bool strict)
    {
        // 루트 우선, 없으면 자식/부모까지
        Transform root = componentRoot ? componentRoot : transform;

        combatStatHolder = root.GetComponent<ICombatStatHolder>() ??
                           root.GetComponentInChildren<ICombatStatHolder>(true) ??
                           GetComponentInChildren<ICombatStatHolder>(true);

        attackBehavior = root.GetComponent<IAttackBehavior>() ??
                           root.GetComponentInChildren<IAttackBehavior>(true) ??
                           GetComponentInChildren<IAttackBehavior>(true);

        hitReactor = root.GetComponent<IHitReactor>() ??
                           root.GetComponentInChildren<IHitReactor>(true) ??
                           GetComponentInChildren<IHitReactor>(true);

        anim = root.GetComponent<IAnimationController>() ??
                           root.GetComponentInChildren<IAnimationController>(true) ??
                           GetComponentInChildren<IAnimationController>(true);

        healthCtrl = root.GetComponent<IHealth>() ??
                           root.GetComponentInChildren<IHealth>(true) ??
                           GetComponentInChildren<IHealth>(true);

        if (strict)
        {
            if (combatStatHolder == null) Debug.LogWarning($"[EnemyController] No ICombatStatHolder on {name}");
            if (attackBehavior == null) Debug.LogWarning($"[EnemyController] No IAttackBehavior on {name}");
            if (anim == null) Debug.LogWarning($"[EnemyController] No IAnimationController on {name}");
            if (healthCtrl == null) Debug.LogWarning($"[EnemyController] No IHealth on {name}");
        }
    }

    private void TryFindPlayer(bool immediate)
    {
        if (!immediate && Time.time < _nextReacquireAt) return;
        GameObject p = null;
        try { p = GameObject.FindGameObjectWithTag(playerTag); } catch { }
        player = p ? p.transform : null;
    }

    private void Halt()
    {
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity;
        v.x = 0f;
        rb.linearVelocity = v;
#else
        var v = rb.velocity;
        v.x = 0f;
        rb.velocity = v;
#endif
    }

    // ----------------- 이동/시선 -----------------

    private void ChaseAndFace()
    {
        if (isDead || player == null || combatStatHolder?.Stats == null) return;

        float dx = player.position.x - CurrentPos.x;
        float dirX = dx > 0.02f ? 1f : (dx < -0.02f ? -1f : 0f);
        float moveSpeed = Mathf.Max(0f, combatStatHolder.Stats.MoveSpeed);

        ApplyFacing(dirX == 0f ? (player.position.x >= CurrentPos.x ? 1f : -1f) : dirX);

        if (isAttacking)
        {
            anim?.SetBool("1_Move", false);
            Halt();
            return;
        }

        if (Mathf.Abs(dx) > stopDistance)
        {
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                var v = rb.linearVelocity;
                v.x = dirX * moveSpeed;
                rb.linearVelocity = v;
#else
                var v = rb.velocity;
                v.x = dirX * moveSpeed;
                rb.velocity = v;
#endif
            }
            else
            {
                // Kinematic 폴백
                transform.Translate(Vector2.right * (dirX * moveSpeed * Time.deltaTime));
            }
            anim?.SetBool("1_Move", true);
        }
        else
        {
            Halt();
            anim?.SetBool("1_Move", false);
        }
    }

    private void ApplyFacing(float dirX)
    {
        bool isRight = dirX >= 0f;

        if (isRight)
        {
            transform.localScale = flipRight;
            lookLeft = !lookLeft;
        }
        else
        {
            transform.localScale = flipLeft;
            lookLeft = !lookLeft;
        }
    }

    // ----------------- 공격 -----------------

    private void TryAttack()
    {
        if (isDead || isAttacking) return;

        // 조립 순서/부착 위치 때문에 공격기가 아직 없을 수 있어 주기적으로 재시도
        if (attackBehavior == null)
        {
            AcquireComponents(strict: false);
            if (attackBehavior == null) return;
        }

        if (Time.time < nextAttackTime) return;
        if (player == null) return;

        float originY = (atkStartPoint ? atkStartPoint.position.y : transform.position.y);
        float deltaY = Mathf.Abs(player.position.y - CurrentPos.y);
        float deltaX = Mathf.Abs(player.position.x - CurrentPos.x);

        float behaviorRange = Mathf.Max(0f, attackBehavior.Range);
        float statRange = combatStatHolder?.Stats != null ? Mathf.Max(0f, combatStatHolder.Stats.AttackRange) : 0f;
        float useRange = Mathf.Max(behaviorRange, statRange);

        // 원거리면 Y축 정렬을 덜 까다롭게(혹은 무시)
        bool isRanged = attackBehavior is RangedAttackBehavior;
        bool yOk = isRanged && ignoreYForRanged ? true : (deltaY <= yThreshold);

        if (yOk && deltaX <= useRange)
        {
            StartCoroutine(AttackRoutine(useRange));
        }
    }

    private IEnumerator AttackRoutine(float useRange)
    {
        isAttacking = true;
        try
        {
            anim?.SetTrigger("2_Attack");
            Halt();

            if (windup > 0f) yield return new WaitForSeconds(windup);

            Vector2 pos = atkStartPoint
                ? (Vector2)atkStartPoint.position
                : (Vector2)(componentRoot ? componentRoot.position : transform.position);

            float dmg = combatStatHolder != null ? combatStatHolder.CalculatePhysicsDmg() : 1f;
            if (TryGetComponent<DamageMultiplier>(out var mul)) dmg = mul.Apply(dmg);

            // 공격 실행 (원거리/근접 공용)
            attackBehavior.Execute(pos, dmg, useRange);

            if (recover > 0f) yield return new WaitForSeconds(recover);
        }
        finally
        {
            isAttacking = false;
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
            if (debugLog) Debug.Log($"[EnemyController] NextAttack @ {nextAttackTime:0.00} ({name})");
        }
    }

    // ----------------- 사망 -----------------

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        anim?.SetTrigger("4_Death");
        anim?.SetBool("isDeath", true);

        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (col) col.enabled = false;

        Destroy(gameObject, 1.5f);
    }
}
