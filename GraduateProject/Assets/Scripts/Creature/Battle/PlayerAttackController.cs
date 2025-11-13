using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ICombatStatHolder))]
[RequireComponent(typeof(IAnimationController))]
public class PlayerAttackController : MonoBehaviour
{
    private ICombatStatHolder stat;
    private IAnimationController anim;
    private IHitReactor hitReactor;
    private float attackDelay = 0.5f;
    private bool canAttack = true;

    [Header("공격")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    private void Awake()
    {
        stat = GetComponent<ICombatStatHolder>();
        anim = GetComponent<IAnimationController>();

        if (stat == null)
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

        // TODO - 원거리 공격 추가 및 Case 예외처리 로직 작성 ㄱㄱ
    }

    private IEnumerator PerformMeleeAttack()
    {
        canAttack = false;

        // 1. 공격 애니메이션 시작
        anim.SetTrigger("2_Attack");

        // 2. 스텟에서 공격 간 딜레이 시간 체크 및 전달
        float delay = stat.Stats.AttackDelay;
        yield return new WaitForSeconds(delay);

        // TODO - Player의 공격 이펙ㅌ트 구현

        // 3.  데미지 전달
        float range = stat.Stats.AttackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayer);
        foreach (var hit in hits)
        {
            var enemyHitReactor = hit.GetComponent<IHitReactor>();
            if (enemyHitReactor != null)
            {
                enemyHitReactor.OnAttacked(stat.CalculatePhysicsDmg());
            }
        }

        // 4. 공격 딜레이 시작
        yield return new WaitForSeconds(attackDelay);
        canAttack = true;
    }
}
