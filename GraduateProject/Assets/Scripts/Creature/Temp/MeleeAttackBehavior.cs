using UnityEngine;

public class MeleeAttackBehavior : MonoBehaviour, IAttackBehavior
{
    [Header("Melee Settings")]
    [SerializeField] private float range = 1.2f;
    [SerializeField] private LayerMask hitLayers;
    public Transform AttackPoint; // 비워두면 현재 위치 사용

    public float Range => range;

    public void Execute(Vector2 position, float dmg, float atkRange)
    {
        float r = atkRange > 0 ? atkRange : range;
        Vector2 center = AttackPoint != null ? (Vector2)AttackPoint.position : position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, hitLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var reactor = hits[i].GetComponent<IHitReactor>();
            if (reactor != null)
            {
                reactor.OnAttacked(dmg); // ✅ 전달받은 dmg 사용
            }
        }

        GetComponent<IAnimationController>()?.SetTrigger("2_Attack");
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.position, range);
        }
    }
}
