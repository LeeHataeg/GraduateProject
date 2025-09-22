using static Define;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Boss/Moves/Jump Smash")]
public class JumpSmashMoveSO : BossMoveSO
{
    public AnimKey jumpKey = AnimKey.JumpAtk;
    public AnimKey landKey = AnimKey.JumpAtkLand;
    public float jumpForce = 10f;
    public float smashDamage = 18f;
    public float smashRadius = 2.2f;
    public LayerMask hitMask;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // 점프
        ctx.Anims.Play(ctx.Anim, jumpKey);
        var v = ctx.RB.linearVelocity; v.y = jumpForce; ctx.RB.linearVelocity = v;
        // 낙하 대기
        while (ctx.RB.linearVelocity.y > -0.01f) yield return null;
        // 착지 순간 처리(간단 판정) — 실제 타이밍은 애니메이션 이벤트가 이상적
        ctx.Anims.Play(ctx.Anim, landKey);
        var p = ctx.Self.position;
        foreach (var h in Physics2D.OverlapCircleAll(p, smashRadius, hitMask))
        {
            if (h.transform == ctx.Self) continue;
            h.GetComponent<IHitReactor>()?.OnAttacked(smashDamage);
        }
    }
}