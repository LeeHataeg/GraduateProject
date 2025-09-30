using System.Collections;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Boss/Moves/Combo Chain (Atk1→Atk2→Atk3)")]
public class ComboChainMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey atk1Key = AnimKey.Atk1;
    public AnimKey atk2Key = AnimKey.Atk2;
    public AnimKey atk3Key = AnimKey.Atk3;
    public AnimKey recoverKey = AnimKey.Recover;
    public AnimKey idleKeyAfterRecover = AnimKey.Idle;

    [Header("Chain Rules")]
    [Tooltip("콤보 유지 X거리(절대값) 최대치 — stopDist보다 넉넉히! (예: 3.0)")]
    public float continueMaxDistance = 3.0f;
    [Tooltip("각 스텝 종료 시 지면에 있어야 다음 단계로 진행")]
    public bool requireGroundedEachStep = false;

    [Header("Safety")]
    public float perStepTimeout = 1.2f;

    [SerializeField] private float distanceSlack = 0.15f; // 경계 흔들림 여유

    // ref 대신 내부 플래그 사용
    private bool _brokeByDistance;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // 1) Atk1
        _brokeByDistance = false;
        yield return PlayStepWithDistanceBreak(ctx, atk1Key, continueMaxDistance, perStepTimeout);
        if (_brokeByDistance || !CanProceedNext(ctx)) 
        { 
            yield return DoRecoverToIdle(ctx, "after Atk1"); 
            yield break; 
        }

        // 2) Atk2
        _brokeByDistance = false;
        yield return PlayStepWithDistanceBreak(ctx, atk2Key, continueMaxDistance, perStepTimeout);
        if (_brokeByDistance || !CanProceedNext(ctx)) 
        { 
            yield return DoRecoverToIdle(ctx, "after Atk2"); 
            yield break; 
        }

        // 3) Atk3
        yield return PlayStep_Atk3_WithEnterCheck(ctx);
        yield return PlayRecoverThenIdle(ctx);

        yield return WaitClipEnd(ctx, perStepTimeout);

        yield return PlayRecoverThenIdle(ctx);
    }

    // 디버그 - 테스트용
    private IEnumerator PlayStep_Atk3_WithEnterCheck(BossContext ctx)
    {
        string before = SafeCurClip(ctx);

        ctx.Anims.Play(ctx.Anim, atk3Key);
        yield return null; // 한 프레임 양보해서 전이 반영

        string now = SafeCurClip(ctx);

#if UNITY_EDITOR
        if (string.IsNullOrEmpty(now))
            Debug.LogWarning("[ComboChain] Atk3: current clip is empty just after Play.");
#endif

        // 1) 클립이 바뀌지 않았다 → Atk3로 못 들어간 것(매핑/상태명 문제일 확률 높음)
        if (string.IsNullOrEmpty(now) || now == before)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ComboChain] Atk3 did NOT start. before='{before}' now='{now}'. " +
                             $"Check AnimMapSO mapping for AnimKey.Atk3 and Animator state name.");
#endif
            yield break; // 상위에서 RecoverThenIdle 호출됨
        }

        // 2) 들어가긴 했지만 바로 튕길 수도 있으니, 최소 한 프레임은 Atk3 유지되는지 체크(선택)
        yield return null;
        string now2 = SafeCurClip(ctx);
        if (now2 != now)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ComboChain] Atk3 interrupted immediately → '{now2}'. " +
                             $"Check AnyState/Atk2→Recover transitions & conditions.");
#endif
            yield break;
        }

        // 3) 정상적으로 Atk3가 돌고 있으니 끝날 때까지 대기
        yield return WaitClipEnd(ctx, perStepTimeout);
    }

    private IEnumerator PlayStepWithDistanceBreak(BossContext ctx, AnimKey stepKey, float limit, float timeout)
    {
        ctx.Anims.Play(ctx.Anim, stepKey);

        string start = SafeCurClip(ctx);
        if (string.IsNullOrEmpty(start)) { yield return null; start = SafeCurClip(ctx); }

        float elapsed = 0f;
        while (!string.IsNullOrEmpty(start) && SameClip(ctx, start))
        {
            if (IsOutOfRangeSlack(ctx, limit, distanceSlack))
            {
#if UNITY_EDITOR
                Debug.Log($"[ComboChain] break to Recover: dx={CurDx(ctx):0.00} > {limit}+{distanceSlack} at {stepKey}");
#endif
                ctx.Anims.Play(ctx.Anim, recoverKey);
                yield return WaitClipEnd(ctx, timeout);
                ctx.Anims.Play(ctx.Anim, idleKeyAfterRecover);
                _brokeByDistance = true; // 플래그 설정
                yield break;
            }
            yield return null;
            elapsed += Time.deltaTime;
            if (timeout > 0f && elapsed >= timeout) break;
        }
    }

    private IEnumerator PlayRecoverThenIdle(BossContext ctx)
    {
        ctx.Anims.Play(ctx.Anim, recoverKey);
        yield return WaitClipEnd(ctx, perStepTimeout);
        ctx.Anims.Play(ctx.Anim, idleKeyAfterRecover);
    }

    private IEnumerator DoRecoverToIdle(BossContext ctx, string where)
    {
#if UNITY_EDITOR
        Debug.Log($"[ComboChain] Recover→Idle ({where}), groundedOK={CanProceedNext(ctx)}");
#endif
        yield return PlayRecoverThenIdle(ctx);
    }

    private bool CanProceedNext(BossContext ctx)
    {
        if (!requireGroundedEachStep || ctx.RB == null) return true;
#if UNITY_6000_0_OR_NEWER
        return Mathf.Abs(ctx.RB.linearVelocity.y) <= 0.05f;
#else
        return Mathf.Abs(ctx.RB.velocity.y) <= 0.05f;
#endif
    }

    // === helpers ===
    private static string SafeCurClip(BossContext ctx)
    {
        var n = ctx.Anim != null ? ctx.Anim.GetCurClipname() : null;
        return string.IsNullOrEmpty(n) ? null : n;
    }
    private static bool SameClip(BossContext ctx, string start)
    {
        var now = ctx.Anim != null ? ctx.Anim.GetCurClipname() : null;
        return !string.IsNullOrEmpty(now) && now == start;
    }
    private static IEnumerator WaitClipEnd(BossContext ctx, float timeout)
    {
        string start = SafeCurClip(ctx);
        if (string.IsNullOrEmpty(start)) { yield return null; start = SafeCurClip(ctx); }

        float elapsed = 0f;
        while (!string.IsNullOrEmpty(start) && SameClip(ctx, start))
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (timeout > 0f && elapsed >= timeout) break;
        }
    }
    private static float CurDx(BossContext ctx)
    {
        if (!ctx.Player) return float.PositiveInfinity;
        return Mathf.Abs(ctx.Player.position.x - ctx.Self.position.x);
    }
    private static bool IsOutOfRangeSlack(BossContext ctx, float limit, float slack)
    {
        return CurDx(ctx) > (limit + Mathf.Max(0f, slack));
    }
}
