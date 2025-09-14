using UnityEngine;

public class MeleeAttackBehavior : MonoBehaviour, IAttackBehavior
{
    [Header("Attack Settings")]
    public float Range = 1.2f;
    public LayerMask HitLayers;
    public Transform AttackPoint;  // 비워두면 position 파라미터/본인 transform 사용

    // IAttackBehavior 명시적 구현
    float IAttackBehavior.Range => Range;

    // IAttackBehavior.Execute 구현
    public void Execute(Vector2 position, float dmg, float atkRange)
    {
        // 실제 사용할 반경: 파라미터 우선, 없으면 기본 Range
        float r = (atkRange > 0f) ? atkRange : Range;

        // 공격 중심점: AttackPoint 있으면 그 위치, 없으면 넘어온 position(없으면 transform)
        Vector2 center = AttackPoint != null ? (Vector2)AttackPoint.position :
                         (position != default ? position : (Vector2)transform.position);

        // 범위 내 타격 대상 검색
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, HitLayers);
        foreach (var h in hits)
        {
            var reactor = h.GetComponent<IHitReactor>();
            if (reactor != null)
            {
                reactor.OnAttacked(dmg);
            }
        }

        // 공격 애니메이션 트리거
        GetComponent<IAnimationController>()?.SetTrigger("2_Attack");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = AttackPoint != null ? AttackPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, Range);
    }
}
