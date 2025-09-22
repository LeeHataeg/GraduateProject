using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Moves/Dash Strike")]
public class DashStrikeMoveSO : BossMoveSO
{
    public float dashSpeed = 9f;
    public float dashTime = 0.35f;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // 플레이어 쪽으로 순간 가속
        float dir = Mathf.Sign(ctx.Player.position.x - ctx.Self.position.x);
        float t = 0f;
        while (t < dashTime)
        {
            var v = ctx.RB.linearVelocity;
            v.x = dir * dashSpeed;
            ctx.RB.linearVelocity = v;
            t += Time.deltaTime;
            yield return null;
        }
        // 멈춤
        var stop = ctx.RB.linearVelocity; stop.x = 0; ctx.RB.linearVelocity = stop;
    }
}