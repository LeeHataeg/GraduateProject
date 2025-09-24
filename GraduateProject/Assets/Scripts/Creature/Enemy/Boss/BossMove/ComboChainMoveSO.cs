using System.Collections;
using UnityEngine;
using static Define;

/// Atk1 -> Atk2 -> Atk3 순서 강제 콤보
/// * 각 단계 사이에 플레이어와의 거리 체크
/// * 거리가 임계값을 넘으면 Recover -> Idle로 종료
[CreateAssetMenu(menuName = "Boss/Moves/Combo Chain (Atk1→Atk2→Atk3)")]
public class ComboChainMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey atk1Key = AnimKey.Atk1;          // Run() 시작 시 base.animKey로도 Atk1이 재생됨
    public AnimKey atk2Key = AnimKey.Atk2;
    public AnimKey atk3Key = AnimKey.Atk3;
    public AnimKey recoverKey = AnimKey.Recover;    // 중단 시 사용
    public AnimKey idleKeyAfterRecover = AnimKey.Idle;

    [Header("Chain Rules")]
    [Tooltip("콤보를 이어가려면 플레이어까지의 X거리(절대값)가 이 값 이하이어야 함")]
    public float continueMaxDistance = 2.6f;

    [Tooltip("공중 콤보 금지 등 필요 시 사용")]
    public bool requireGroundedEachStep = true;

    [Header("Fallback Durations (애니 이벤트 없을 때 사용)")]
    [Tooltip("Atk1 재생 후 다음 단계로 넘어가기까지 대기 시간")]
    public float atk1StepTime = 0.30f;
    public float atk2StepTime = 0.35f;
    public float atk3StepTime = 0.40f;
    public float recoverTime = 0.35f;

    // BossMoveSO.Run()이 선딜/후딜/쿨다운, 이동락을 처리한다.
    // 이 Execute에서는 단계별 애니만 재생하고 거리 체크를 수행한다.
    protected override IEnumerator Execute(BossContext ctx)
    {
        // 1) Atk1 (Run()에서 이미 animKey가 atk1Key로 재생되지만 안전하게 한 번 더)
        ctx.Anims.Play(ctx.Anim, atk1Key);
        yield return WaitStep(atk1StepTime, ctx);

        // → 거리/상태 체크 후 안 되면 Recover
        if (!CanContinueToNext(ctx))
        {
            yield return DoRecover(ctx);
            yield break;
        }

        // 2) Atk2
        ctx.Anims.Play(ctx.Anim, atk2Key);
        yield return WaitStep(atk2StepTime, ctx);

        if (!CanContinueToNext(ctx))
        {
            yield return DoRecover(ctx);
            yield break;
        }

        // 3) Atk3
        ctx.Anims.Play(ctx.Anim, atk3Key);
        yield return WaitStep(atk3StepTime, ctx);

        // 콤보 완주 후에는 BossController가 다음 무브를 고른다(별도 처리 없음)
        yield break;
    }

    private IEnumerator DoRecover(BossContext ctx)
    {
        if (recoverKey != AnimKey.Idle) // 혹시 Recover 키가 비어있으면 Idle만
        {
            ctx.Anims.Play(ctx.Anim, recoverKey);
            if (recoverTime > 0f) yield return new WaitForSeconds(recoverTime);
        }
        ctx.Anims.Play(ctx.Anim, idleKeyAfterRecover);
    }

    private bool CanContinueToNext(BossContext ctx)
    {
        if (ctx.Player == null) return false;
        float dx = Mathf.Abs(ctx.Player.position.x - ctx.Self.position.x);
        if (dx > continueMaxDistance) return false;
        if (requireGroundedEachStep && Mathf.Abs(ctx.RB.linearVelocity.y) > 0.05f) return false;
        return true;
        // 필요하면 여기서 시야/높이(Y차) 등 추가 조건도 체크 가능
    }

    private static IEnumerator WaitStep(float seconds, BossContext ctx)
    {
        // 애니메이션 이벤트로 타이밍을 끊는다면 이 대기 대신 AE에서 다음 단계 호출로 바꿔도 된다.
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
    }
}
