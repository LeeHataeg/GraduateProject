using System.Collections;
using UnityEngine;
using static Define;

/// P1 전용: ChargeJump → JumpIn → (하강 중 JumpAtkLoop=true) → JumpAtkLand → Loop=false
/// * 데미지는 애니메이션 이벤트로만 처리(예: Land 클립에 AE_HitBegin/End)
[CreateAssetMenu(menuName = "Boss/Moves/Jump Chain (Charge→JumpIn→Loop→Land)")]
public class JumpChainMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey chargeKey = AnimKey.ChargeJump; // 선택: 없으면 비워두세요(=JumpIn부터 시작)
    public AnimKey jumpInKey = AnimKey.JumpIn;
    public AnimKey loopBoolKey = AnimKey.JumpAtkLoop; // Bool 루프
    public AnimKey landKey = AnimKey.JumpAtkLand;

    [Header("Timings")]
    public float chargeTime = 0.25f; // ChargeJump 머무는 시간(없으면 0)
    public float loopMinTime = 0.10f; // 하강 루프 최소 유지시간(이벤트 없을 때 안전망)

    [Header("Jump Motion")]
    public float jumpForce = 10f;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // 1) Charge (옵션)
        if (chargeTime > 0f && chargeKey != AnimKey.Idle)
        {
            ctx.Anims.Play(ctx.Anim, chargeKey);
            yield return new WaitForSeconds(chargeTime);
        }

        // 2) JumpIn & 발사
        ctx.Anims.Play(ctx.Anim, jumpInKey);
        var v = ctx.RB.linearVelocity; v.y = jumpForce; ctx.RB.linearVelocity = v;

        // 3) Loop on (하강 중)
        ctx.Anims.Play(ctx.Anim, loopBoolKey, true);

        // 하강 시작될 때까지 대기
        while (ctx.RB.linearVelocity.y > -0.01f) yield return null;

        // 최소 루프 시간 보장
        if (loopMinTime > 0f) yield return new WaitForSeconds(loopMinTime);

        // 4) Land (여기서 애니메이션 이벤트로 히트박스 on/off)
        ctx.Anims.Play(ctx.Anim, landKey);

        // 루프 off
        ctx.Anims.Play(ctx.Anim, loopBoolKey, false);
    }
}
