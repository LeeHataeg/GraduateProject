using UnityEngine;
using System.Collections;
using static Define;

[CreateAssetMenu(menuName = "Boss/Moves/Idle Reactive Block")]
public class BlockReactiveFromIdleMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey toBlocked = AnimKey.ToBlock;
    public AnimKey blocked = AnimKey.Blocked;
    public AnimKey outBlocked = AnimKey.OutBlocked;

    [Header("Windows")]
    public float reactWindow = 0.6f;     // ToBlocked 동안 데미지 들어오면 Blocked
    public float blockedShow = 0.25f;
    public float outBlockedShow = 0.2f;

    public override bool CanRun(BossContext ctx)
    {
        // Idle 도중 “피격 누적치가 임계”라는 외부 의사결정을 못 받으니
        // PhaseSO 가중치/랜덤으로 언제든 나올 수 있도록 기본 true (CD로 제어)
        return base.CanRun(ctx);
    }

    protected override IEnumerator Execute(BossContext ctx)
    {
        float hp0 = ctx.Health != null ? ctx.Health.CurrentHp : 999999f;

        ctx.Anims.Play(ctx.Anim, toBlocked);
        float t = 0f;
        bool gotHit = false;

        while (t < reactWindow)
        {
            t += Time.deltaTime;
            if (ctx.Health != null && ctx.Health.CurrentHp < hp0) { gotHit = true; break; }
            yield return null;
        }

        if (gotHit)
        {
            ctx.Anims.Play(ctx.Anim, blocked);
            yield return new WaitForSeconds(blockedShow);
        }
        else
        {
            ctx.Anims.Play(ctx.Anim, outBlocked);
            yield return new WaitForSeconds(outBlockedShow);
        }
    }
}
