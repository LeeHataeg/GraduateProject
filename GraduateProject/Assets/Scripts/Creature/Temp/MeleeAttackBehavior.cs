using UnityEngine;

[RequireComponent(typeof(BaseCharacter))]
public class MeleeAttackBehavior : MonoBehaviour
{
    [Header("Attack Settings")]
    public float Range = 1.2f;
    public LayerMask HitLayers;
    public Transform AttackPoint;  // 빈 GameObject로 위치 찍어두세요

    public float Damage => GetComponent<BaseStat>().Attack;


    public void Execute(Vector2 position, float dmg, float atkRange)
    {
        // 범위 내 적 검색
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            position,
            atkRange,
            HitLayers
        );
        foreach (var h in hits)
        {
            var reactor = h.GetComponent<IHitReactor>();
            if (reactor != null)
            {
                Vector2 toTarget = (Vector2)h.transform.position - position;
                reactor.OnAttacked(Damage);
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