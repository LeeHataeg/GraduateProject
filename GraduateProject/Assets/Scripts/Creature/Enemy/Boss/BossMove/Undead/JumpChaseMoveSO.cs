using UnityEngine;
using System.Collections;
using static Define;

[CreateAssetMenu(menuName = "Boss/Moves/Jump Chase")]
public class JumpChaseMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey toJump = AnimKey.ToJump;
    public AnimKey jumping = AnimKey.Jumping;
    public AnimKey toFall = AnimKey.ToFall;
    public AnimKey falling = AnimKey.Falling;
    public AnimKey land = AnimKey.Land;

    [Header("Jump Physics")]
    public float jumpUpVelocity = 9f;        // 점프 초속
    public float airHSpeed = 3.2f;           // 공중 가로 이동 속도(지상보다 감속)
    public float maxJumpWait = 1.0f;         // 상승→하강 전환까지 최대 대기

    protected override IEnumerator Execute(BossContext ctx)
    {
        var rb = ctx.RB;
        if (!rb) yield break;

        // ToJump (1회)
        ctx.Anims.Play(ctx.Anim, toJump);
        yield return new WaitForSeconds(0.05f);

        // Jumping: 초속 부여 + 공중에서 x방향 감속 이동
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.y = jumpUpVelocity; rb.linearVelocity = v;
#else
        var v = rb.velocity; v.y = jumpUpVelocity; rb.velocity = v;
#endif
        ctx.Anims.Play(ctx.Anim, jumping);

        float t = 0f;
        while (t < maxJumpWait)
        {
            t += Time.fixedDeltaTime;

            // 플레이어 방향으로 감속 이동
            if (ctx.Player)
            {
                float dx = ctx.Player.position.x - ctx.Self.position.x;
#if UNITY_6000_0_OR_NEWER
                var vv = rb.linearVelocity;
#else
                var vv = rb.velocity;
#endif
                vv.x = Mathf.MoveTowards(vv.x, Mathf.Sign(dx) * airHSpeed, 8f * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = vv;
#else
                rb.velocity = vv;
#endif
            }

            // 하강으로 전환(중력에 맡겨 y<=0 되는 시점)
#if UNITY_6000_0_OR_NEWER
            if (rb.linearVelocity.y <= 0f) break;
#else
            if (rb.velocity.y <= 0f) break;
#endif
            yield return new WaitForFixedUpdate();
        }

        // ToFall (1회)
        ctx.Anims.Play(ctx.Anim, toFall);
        yield return new WaitForSeconds(0.05f);

        // Falling: 땅 닿을 때까지
        bool grounded = false;
        var boss = ctx.Self.GetComponent<BossController>();
        System.Action onGround = () => grounded = true;
        if (boss != null) boss.GroundTouched += onGround;

        ctx.Anims.Play(ctx.Anim, falling);
        float watchdog = 2.0f;
        while (!grounded && watchdog > 0f)
        {
            watchdog -= Time.deltaTime;
            yield return null;
        }
        if (boss != null) boss.GroundTouched -= onGround;

        // Land(1회)
        ctx.Anims.Play(ctx.Anim, land);
        yield return new WaitForSeconds(0.1f);
    }
}
