using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy의 공격 입력을 받아 근접 공격 판정, 애니메이션, 데미지 전달을 담당합니다.
/// </summary>
[RequireComponent(typeof(ICombatStatHolder))]
[RequireComponent(typeof(IAnimationController))]
public class EnemyAttackController : MonoBehaviour
{
    private ICombatStatHolder statHolder;
    private IAnimationController anim;
    private float attackCooldown = 0.5f;
    private bool canAttack = true;

    [Header("공격 설정")]
    [Tooltip("공격 범위를 검사할 기준 위치")]
    [SerializeField] private Transform attackPoint;
    [Tooltip("공격 시 플레이어를 구분할 Layer")]
    [SerializeField] private LayerMask playerLayer;

    private void Awake()
    {
        statHolder = GetComponent<ICombatStatHolder>();
        anim = GetComponent<IAnimationController>();

        if (statHolder == null)
            Debug.LogError($"[{nameof(EnemyAttackController)}] ICombatStatHolder를 찾을 수 없습니다.");
        if (anim == null)
            Debug.LogError($"[{nameof(EnemyAttackController)}] IAnimationController를 찾을 수 없습니다.");
        if (attackPoint == null)
            Debug.LogError($"[{nameof(EnemyAttackController)}] attackPoint가 할당되지 않았습니다. 인스펙터에서 지정해주세요.");
    }

    /// <summary>
    /// 외부(예: EnemyController)에서 공격을 트리거할 때 호출합니다.
    /// </summary>
    public void TriggerAttack()
    {
        if (!canAttack) return;
        StartCoroutine(PerformMeleeAttack());
    }

    private IEnumerator PerformMeleeAttack()
    {
        canAttack = false;

        // (1) 공격 애니메이션 재생
        anim.SetTrigger("2_Attack");

        // (2) 스탯에 정의된 Delay 만큼 대기
        yield return new WaitForSeconds(statHolder.Stats.AttackDelay);

        // (3) 공격 범위 내 플레이어 탐지 및 대미지 전달
        float range = statHolder.Stats.AttackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, playerLayer);
        foreach (var hit in hits)
        {
            var playerHitReactor = hit.GetComponent<IHitReactor>();
            if (playerHitReactor != null)
            {
                // 타격 방향 계산
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                playerHitReactor.OnAttacked(statHolder.CalculatePhysicsDmg());
            }
        }

        // (4) 쿨다운 대기
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, statHolder != null ? statHolder.Stats.AttackRange : 0f);
    }
}
