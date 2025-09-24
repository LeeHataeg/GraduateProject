using static Define;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Boss/Moves/Jump Smash")]
public class JumpSmashMoveSO : BossMoveSO
{
    public AnimKey jumpKey = AnimKey.JumpIn;
    public AnimKey landKey = AnimKey.JumpAtkLand;
    public AnimKey loopBoolKey = AnimKey.JumpAtkLoop; // Bool 루프
    public float jumpForce = 10f;
    public float smashDamage = 18f;
    public float smashRadius = 2.2f;
    public LayerMask hitMask;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // 점프 시작
        ctx.Anims.Play(ctx.Anim, jumpKey);
        ctx.Anims.Play(ctx.Anim, loopBoolKey, true); // 하강 중 루프 on

        var v = ctx.RB.linearVelocity; v.y = jumpForce; ctx.RB.linearVelocity = v;

        // 정점 지나 낙하 시작되면 대기
        while (ctx.RB.linearVelocity.y > -0.01f) yield return null;

        // 착지
        ctx.Anims.Play(ctx.Anim, landKey);
        ctx.Anims.Play(ctx.Anim, loopBoolKey, false); // 루프 off

        var p = ctx.Self.position;
        foreach (var h in Physics2D.OverlapCircleAll(p, smashRadius, hitMask))
        {
            if (h.transform == ctx.Self) continue;
            h.GetComponent<IHitReactor>()?.OnAttacked(smashDamage);
        }
    }
}
