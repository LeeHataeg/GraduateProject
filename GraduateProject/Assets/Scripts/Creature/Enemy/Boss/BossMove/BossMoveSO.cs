using static Define;
using UnityEngine;
using System.Collections;

public abstract class BossMoveSO : ScriptableObject
{
    [Header("Common")]
    public AnimKey animKey = AnimKey.LightAtk;
    [Tooltip("시작 전 선딜")]
    public float windup = 0.05f;
    [Tooltip("행동 종료 후 후딜")]
    public float recover = 0.1f;
    [Tooltip("행동 재사용 대기")]
    public float cooldown = 1.0f;
    [Tooltip("실행 동안 이동 잠금")]
    public bool lockMovement = true;

    [Header("조건")]
    public float minRange = 0f;
    public float maxRange = 99f;
    public bool requireGrounded = false;

    private float _nextReadyTime;

    public bool CanRun(BossContext ctx)
    {
        if (Time.time < _nextReadyTime) return false;
        if (ctx == null || ctx.Player == null) return false;

        var dx = Mathf.Abs(ctx.Player.position.x - ctx.Self.position.x);
        if (dx < minRange || dx > maxRange) return false;
        if (requireGrounded && Mathf.Abs(ctx.RB.linearVelocity.y) > 0.05f) return false;

        return ExtraCanRun(ctx);
    }

    protected virtual bool ExtraCanRun(BossContext ctx) => true;

    public IEnumerator Run(BossContext ctx)
    {
        _nextReadyTime = Time.time + cooldown;

        if (lockMovement) ctx.LockMove(true);
        ctx.Anims.Play(ctx.Anim, animKey);

        if (windup > 0) yield return new WaitForSeconds(windup);
        yield return Execute(ctx);
        if (recover > 0) yield return new WaitForSeconds(recover);

        if (lockMovement) ctx.LockMove(false);
    }

    protected abstract IEnumerator Execute(BossContext ctx);
}