using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ICombatStatHolder))]
[RequireComponent(typeof(IHealth))]
[RequireComponent(typeof(IHitReactor))]
[RequireComponent(typeof(IAnimationController))]
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
    [SerializeField] private Transform atkStartPoint;

    private bool isDead = false;
    private bool isAttacking = false;

    // y 축 비교 오차 허용 범위
    private float yThreshold = 0.7f;

    private void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.Log("플레이어 Tag로 못찾아연");
    }

    private void Awake()
    {
        combatStatHolder = GetComponent<ICombatStatHolder>();
        attackBehavior = GetComponent<IAttackBehavior>();
        hitReactor = GetComponent<IHitReactor>();
        anim = GetComponent<IAnimationController>();
        healthCtrl = GetComponent<IHealth>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        //if (combatStatHolder == null || attackBehavior == null || hitReactor == null ||
        //    anim == null || healthCtrl == null || rb == null || col == null)
        //{
        //    Debug.LogError($"[{nameof(EnemyController)}] 필수 컴포넌트가 누락");
        //}
    }

    private void Start()
    {
        // 사망 시 HandleDeath가 호출되도록 구독
        healthCtrl.OnDead += HandleDeath;
    }

    private void Update()
    {
        if (isDead || player == null || combatStatHolder.Stats == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // (1) 탐지 반경 내인가
        if (distToPlayer <= combatStatHolder.Stats.DetectionRadius)
        {
            Chase();

            // (2) 공격 시도
            TryAttack();
        }
        else
        {
            // 탐지 범위 밖일 때 Idle 상태(애니메이션)
            anim.SetBool("1_Move", false);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    public void Hit()
    {
        // 외부에서 Hit()을 호출하면 데미지를 입도록 호출
        // 만약 적이 데미지를 처리하는 별도 로직이 필요하다면 추가 구현
        // ex) HealthController.TakeDamage(...) 등을 직접 호출할 수 있음
    }

    private void Chase()
    {
        if (isDead || isAttacking)
        {
            anim.SetBool("1_Move", false);
            return;
        }

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        float moveSpeed = combatStatHolder.Stats.MoveSpeed;

        // (1) 이동
        rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);

        // (2) 이동 애니메이션
        anim.SetBool("1_Move", true);

        // (3) 스프라이트 좌우 반전 (필요 시)
        if (dirX > 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (dirX < 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
    }

    private void TryAttack()
    {
        if (isDead || isAttacking || attackBehavior == null)
        {
            return;
        }

        float deltaY = Mathf.Abs(player.position.y - transform.position.y);
        float deltaX = Mathf.Abs(player.position.x - transform.position.x);

        // y축이 일정 범위 내이고, x축 거리가 공격 범위 이내인지 확인
        if (deltaY <= yThreshold && deltaX <= attackBehavior.Range)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        Debug.Log("PerformMeleeAttack 진입!");

        isAttacking = true;
        // (1) 공격 애니메이션 재생
        anim.SetTrigger("2_Attack");

        // (2) 공격 딜레이(애니메이션 끝나길 대기)
        float delay = combatStatHolder.Stats.AttackDelay;
        yield return new WaitForSeconds(delay);

        attackBehavior.Execute(atkStartPoint.position, combatStatHolder.CalculatePhysicsDmg(), combatStatHolder.Stats.AttackRange);

        isAttacking = false;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        // (1) 사망 애니메이션
        anim.SetTrigger("4_Death");
        anim.SetBool("isDeath", true);

        // (2) 이동 및 물리 비활성화
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;

        // (4) 일정 시간 뒤 오브젝트 제거 (애니메이션 길이에 맞춤)
        Destroy(gameObject, 1.5f);
    }
}