using UnityEngine;

[RequireComponent(typeof(BaseCharacter))]
public class MeleeAttackBehavior : MonoBehaviour
{
    [Header("Attack Settings")]
    public float Range = 1.2f;
    public LayerMask HitLayers;
    public Transform AttackPoint;  // 빈 GameObject로 위치 찍어두세요

    // IAttackBehavior
    //public float Range => Range;
    public float Damage => GetComponent<BaseStat>().Attack;

    //float IAttackBehavior.Range => throw new System.NotImplementedException();

    public void Execute(AttackContext context)
    {
        // 범위 내 적 검색
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            context.Origin,
            Range,
            HitLayers
        );
        foreach (var h in hits)
        {
            var reactor = h.GetComponent<IHitReactor>();
            if (reactor != null)
            {
                Vector2 toTarget = (Vector2)h.transform.position - context.Origin;
                reactor.OnAttack(Damage, toTarget.normalized);
            }
        }

        // 공격 애니메이션
        GetComponent<IAnimationController>()?.SetTrigger("2_Attack");
    }

    void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.position, Range);
        }
    }
}