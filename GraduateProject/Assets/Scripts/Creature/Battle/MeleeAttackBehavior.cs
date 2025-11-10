using UnityEngine;

public class MeleeAttackBehavior : MonoBehaviour, IAttackBehavior
{
    [Header("Melee Settings")]
    [SerializeField] private float range = 1.2f;

    [Tooltip("공격이 맞을 레이어 마스크 (비어있으면 기본값으로 Player/PlayerHurtbox 자동 세팅)")]
    [SerializeField] private LayerMask hitLayers;

    [Tooltip("원하는 경우 별도의 공격 기준점 (없으면 Execute(position) 사용)")]
    public Transform AttackPoint;

    public float Range => range;

    /// <summary>외부에서 레이어 마스크를 지정하고 싶을 때 호출</summary>
    public void Configure(LayerMask hitLayers) => this.hitLayers = hitLayers;

    public void SetAttackPoint(Transform t) => AttackPoint = t;

    public void Execute(Vector2 position, float dmg, float atkRange)
    {
        // 1) 실사용 사거리
        float r = atkRange > 0 ? atkRange : range;

        // 2) 기준 위치
        Vector2 center = AttackPoint != null ? (Vector2)AttackPoint.position : position;

        // 3) 마스크 비었으면 기본값 자동 구성 (Player / PlayerHurtbox)
        if (hitLayers.value == 0)
        {
            int p = LayerMask.NameToLayer("Player");
            int ph = LayerMask.NameToLayer("PlayerHurtbox");
            int mask = 0;
            if (p >= 0 && p < 32) mask |= (1 << p);
            if (ph >= 0 && ph < 32) mask |= (1 << ph);

            // 최후의 안전망: 둘 다 없으면 Everything로라도 탐색(디버그에 의존)
            hitLayers = mask != 0 ? mask : ~0;
        }

        // 4) 타격 판정
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, hitLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;

            // 먼저 같은 GO에서 찾고, 없으면 부모에서 다시 찾는다(★ 핵심 보강)
            var reactor = hits[i].GetComponent<IHitReactor>();
            if (reactor == null)
                reactor = hits[i].GetComponentInParent<IHitReactor>();

            if (reactor != null)
            {
                reactor.OnAttacked(dmg);
            }
#if UNITY_EDITOR
            else
            {
                // 디버깅에 유용: 어느 콜라이더가 잡혔는데 리액터를 못 찾는지 표시
                // Debug.Log($"[Melee] No IHitReactor on {hits[i].name} (layer={LayerMask.LayerToName(hits[i].gameObject.layer)})", hits[i]);
            }
#endif
        }

        // 5) (선택) 자체 애니메이션 트리거
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
