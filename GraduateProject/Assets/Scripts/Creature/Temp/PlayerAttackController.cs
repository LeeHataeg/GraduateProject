using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어의 공격 입력을 받아 근접 공격 판정, 애니메이션, 데미지 전달을 담당합니다.
/// </summary>
[RequireComponent(typeof(ICombatStatHolder))]
[RequireComponent(typeof(IAnimationController))]
public class PlayerAttackController : MonoBehaviour
{
    private ICombatStatHolder statHolder;
    private IAnimationController anim;
    private IHitReactor hitReactor; // 범위 내 적에게 데미지 전달 시 사용
    private float attackCooldown = 0.5f;
    private bool canAttack = true;

    [Header("공격 설정")]
    [Tooltip("공격 범위를 검사할 기준 위치 (플레이어 발끝 등)")]
    [SerializeField] private Transform attackPoint;
    [Tooltip("공격 시 적을 구분할 Layer")]
    [SerializeField] private LayerMask enemyLayer;

    private void Awake()
    {
        statHolder = GetComponent<ICombatStatHolder>();
        anim = GetComponent<IAnimationController>();

        if (statHolder == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] ICombatStatHolder를 찾을 수 없습니다.");
        if (anim == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] IAnimationController를 찾을 수 없습니다.");
        if (attackPoint == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] attackPoint가 할당되지 않았습니다. 인스펙터에서 지정해주세요.");
    }

    private void OnEnable()
    {
        // PlayerInputController에서 OnHitEvent(bool)을 발생하도록 미리 구현되어 있어야 합니다.
        var inputController = GetComponent<PlayerInputController>();
        if (inputController != null)
            inputController.OnHitEvent += HandleAttackInput;
        else
            Debug.LogWarning($"[{nameof(PlayerAttackController)}] PlayerInputController를 찾을 수 없습니다. 공격 입력이 불가능합니다.");
    }

    private void OnDisable()
    {
        var inputController = GetComponent<PlayerInputController>();
        if (inputController != null)
            inputController.OnHitEvent -= HandleAttackInput;
    }

    private void HandleAttackInput(bool isPressed)
    {
        if (!isPressed || !canAttack) return;
        StartCoroutine(PerformMeleeAttack());
    }

    private IEnumerator PerformMeleeAttack()
    {
        canAttack = false;

        // (1) 공격 애니메이션 재생
        anim.Play("2_Attack");

        // (2) 공격 딜레이(스탯에서 가져온 Delay 대기)
        float delay = statHolder.Stats.AttackDelay;
        yield return new WaitForSeconds(delay);

        // (3) 공격 범위 내의 적 찾기
        float range = statHolder.Stats.AttackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayer);
        foreach (var hit in hits)
        {
            // 적에게 데미지 및 넉백 전달
            Vector2 dir = (hit.transform.position - transform.position).normalized;
            var enemyHitReactor = hit.GetComponent<IHitReactor>();
            if (enemyHitReactor != null)
            {
                enemyHitReactor.OnAttack(statHolder.Stats.MeleeDamage, dir);
            }
        }

        // (4) 콜드타임 대기
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
