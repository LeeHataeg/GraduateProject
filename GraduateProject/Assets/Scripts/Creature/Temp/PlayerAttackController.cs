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

        // (1) 공격 애니메이션
        anim.SetTrigger("2_Attack");

        // (2) 스탯 딜레이
        float delay = statHolder.Stats.AttackDelay;
        yield return new WaitForSeconds(delay);

        // (2.5) 시각 이펙트 (선택)
        // Resources/Prefabs/Melee_Attck_Effect 프리팹이 있으면 스폰
        GameObject fx = Resources.Load<GameObject>("Prefabs/Melee_Attck_Effect");
        if (fx != null && attackPoint != null)
        {
            // 좌우 바라보는 방향 기반으로 간단 회전 (오른쪽: 0도, 왼쪽: 180도)
            float dirX = transform.localScale.x >= 0 ? 1f : -1f;
            Quaternion rot = (dirX >= 0)
                ? Quaternion.identity
                : Quaternion.Euler(0, 0, 180f);

            var inst = Instantiate(fx, attackPoint.position, rot);
            // 이펙트가 데미지를 표시/연출용으로 쓰는 경우만 세팅
            var fxCtrl = inst.GetComponent<EffectController>();
            if (fxCtrl != null)
                fxCtrl.SetDmg(statHolder.CalculatePhysicsDmg());
        }

        // (3) 판정 및 데미지 전달
        float range = statHolder.Stats.AttackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayer);
        foreach (var hit in hits)
        {
            var enemyHitReactor = hit.GetComponent<IHitReactor>();
            if (enemyHitReactor != null)
            {
                enemyHitReactor.OnAttacked(statHolder.CalculatePhysicsDmg());
            }
        }

        // (4) 쿨다운
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
