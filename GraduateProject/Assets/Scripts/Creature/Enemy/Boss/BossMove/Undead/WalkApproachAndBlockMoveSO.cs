using UnityEngine;
using System.Collections;
using static Define;

[CreateAssetMenu(menuName = "Boss/Moves/Walk Approach + Block Variants")]
public class WalkApproachAndBlockMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey toWalk = AnimKey.ToWalk;
    public AnimKey walkLoop = AnimKey.Walk;
    public AnimKey walkToIdle = AnimKey.WalkToIdle;

    public AnimKey walkBlocking = AnimKey.WalkBlocking;
    public AnimKey walkBLocking2 = AnimKey.WalkBLocking2; // 제작자 표기 그대로
    public AnimKey walkBlocked = AnimKey.WalkBlocked;
    public AnimKey outBlocked = AnimKey.OutBlocked;

    [Header("Approach")]
    public float approachRangeX = 2.0f; // x축 공격 사거리 도달 기준
    public float walkSpeed = 4.5f;
    public float walkAccel = 14f;

    [Header("Damage Thresholds")]
    public float dmgWhileWalking = 10f;   // Walk 중 누적 데미지 임계
    public float dmgWhileOthers = 6f;     // Walk 제외 상태에서의 임계

    [Header("Blocking Windows")]
    public float blockingDuration = 1.2f; // WalkBlocking/WalkBLocking2 시전 최대 시간
    public float blockedShowTime = 0.3f;  // WalkBlocked 1회 표시 시간
    public float outBlockedShowTime = 0.25f;

    public override bool CanRun(BossContext ctx)
    {
        if (!base.CanRun(ctx)) return false;
        if (!ctx.Player) return false;
        // x축 사거리 밖일 때만 유효
        float dx = Mathf.Abs(ctx.Player.position.x - ctx.Self.position.x);
        return dx > approachRangeX;
    }

    protected override IEnumerator Execute(BossContext ctx)
    {
        var rb = ctx.RB; if (!rb) yield break;
        var anims = ctx.Anims;

        // ToWalk
        anims.Play(ctx.Anim, toWalk);
        yield return new WaitForSeconds(0.05f);

        // Walk 루프: 사거리 진입까지
        float startHp = ctx.Health != null ? ctx.Health.CurrentHp : 999999f;
        float accDmgWalk = 0f, accDmgOther = 0f;

        anims.Play(ctx.Anim, walkLoop);
        while (true)
        {
            if (!ctx.Player) break;

            float dx = ctx.Player.position.x - ctx.Self.position.x;
            float adx = Mathf.Abs(dx);

            // 접근 완료?
            if (adx <= approachRangeX) break;

            // 워킹 이동
#if UNITY_6000_0_OR_NEWER
            var v = rb.linearVelocity;
#else
            var v = rb.velocity;
#endif
            float target = Mathf.Sign(dx) * walkSpeed;
            v.x = Mathf.MoveTowards(v.x, target, walkAccel * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif

            // 누적 데미지 추적(샘플링)
            if (ctx.Health != null)
            {
                float nowHp = ctx.Health.CurrentHp;
                float taken = Mathf.Max(0f, startHp - nowHp);
                // 간단히: 프레임당 증가분만큼 워킹 누적치에 반영
                accDmgWalk = taken;
            }

            // 워킹 중 임계 초과 → 블로킹 패턴 분기
            if (accDmgWalk >= Mathf.Max(0.01f, dmgWhileWalking))
            {
                bool pickAlt = Random.value < 0.5f;
                yield return BlockingRoutine(ctx, pickAlt ? walkBLocking2 : walkBlocking);
                // 블록 결과 후 Idle로 복귀했으니 끝
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }

        // WalkToIdle → Idle
        ctx.Anims.Play(ctx.Anim, walkToIdle);
        yield return new WaitForSeconds(0.05f);
    }

    private IEnumerator BlockingRoutine(BossContext ctx, AnimKey which)
    {
        var rb = ctx.RB; if (!rb) yield break;

        // Walk → Blocking 변환 즉시 수행
        ctx.Anims.Play(ctx.Anim, which);

        float hp0 = ctx.Health != null ? ctx.Health.CurrentHp : 999999f;
        float t = 0f;

        while (t < blockingDuration)
        {
            t += Time.deltaTime;

            // 블록 중에도 접근(감속 이동)
            if (ctx.Player && rb)
            {
                float dx = ctx.Player.position.x - ctx.Self.position.x;
#if UNITY_6000_0_OR_NEWER
                var v = rb.linearVelocity;
#else
                var v = rb.velocity;
#endif
                float target = Mathf.Sign(dx) * (walkSpeed * 0.6f);
                v.x = Mathf.MoveTowards(v.x, target, (walkAccel * 0.6f) * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = v;
#else
                rb.velocity = v;
#endif

                // x축 사거리 도달 즉시 OutBlocked
                if (Mathf.Abs(dx) <= approachRangeX)
                {
                    ctx.Anims.Play(ctx.Anim, AnimKey.OutBlocked);
                    yield return new WaitForSeconds(outBlockedShowTime);
                    yield break;
                }
            }

            // 플레이어 공격 들어왔나?(HP 감소로 판정)
            if (ctx.Health != null && ctx.Health.CurrentHp < hp0)
            {
                // 피격 순간 바로 WalkBlocked 1회
                ctx.Anims.Play(ctx.Anim, AnimKey.WalkBlocked);
                yield return new WaitForSeconds(blockedShowTime);
                yield break;
            }

            yield return null;
        }

        // 시간 경과 → OutBlocked
        ctx.Anims.Play(ctx.Anim, AnimKey.OutBlocked);
        yield return new WaitForSeconds(outBlockedShowTime);
    }
}
