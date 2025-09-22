using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Moves/Melee Sweep")]
public class MeleeSweepMoveSO : BossMoveSO
{
    public float damage = 12f;
    public float hitRadius = 1.6f;
    public LayerMask hitMask;
    public Transform hitOriginOffset; // 비워두면 ctx.AttackOrigin

    protected override IEnumerator Execute(BossContext ctx)
    {
        var origin = hitOriginOffset ? hitOriginOffset.position : ctx.AttackOrigin.position;
        var hits = Physics2D.OverlapCircleAll(origin, hitRadius, hitMask);
        foreach (var h in hits)
        {
            if (h.transform == ctx.Self) continue;
            h.GetComponent<IHitReactor>()?.OnAttacked(damage);
        }
        yield break;
    }
}
