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
    [Tooltip("각 스텝별 최대 대기 시간(초). 전이 막힘/루프 방지용 세이프티넷")]
    public float perStepTimeout = 1.2f;

    [SerializeField] private float distanceSlack = 0.15f; // 경계 흔들림 여유

    [Header("Atk3 진입 실패 시 재시도 옵션")]
    [Tooltip("Atk3로 첫 전이가 막히면 1프레임 후 한 번 더 재생을 시도합니다.")]
    public bool retryEnterAtk3IfBlocked = true;

    private bool _brokeByDistance;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // === 콤보 동안 이동/로코모션 덮어쓰기 차단 ===
        var bc = ctx.Self ? ctx.Self.GetComponent<BossController>() : null;
        if (bc) bc.SetMoveLocked(true);

        try
        {
            // 1) Atk1
            _brokeByDistance = false;
            yield return PlayStepWithDistanceBreak_StateBased(ctx, atk1Key, continueMaxDistance, perStepTimeout);
            if (_brokeByDistance || !CanProceedNext(ctx))
            {
#if UNITY_EDITOR
                Debug.Log($"[ComboChain] Recover→Idle (after Atk1), groundedOK={CanProceedNext(ctx)}");
#endif
                yield return PlayRecoverThenIdle_StateBased(ctx);
                yield break;
            }

            // 2) Atk2
            _brokeByDistance = false;
            yield return PlayStepWithDistanceBreak_StateBased(ctx, atk2Key, continueMaxDistance, perStepTimeout);
            if (_brokeByDistance || !CanProceedNext(ctx))
            {
#if UNITY_EDITOR
                Debug.Log($"[ComboChain] Recover→Idle (after Atk2), groundedOK={CanProceedNext(ctx)}");
#endif
                yield return PlayRecoverThenIdle_StateBased(ctx);
                yield break;
            }

            // 3) Atk3 (진입 확인 + 즉시 인터럽트 감지 + 끝까지 대기)
            bool entered = false;
            yield return PlayStep_Atk3_WithEnterCheck_StateBased(ctx, () => entered = true);

            if (!entered)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[ComboChain] Atk3 not entered → Recover→Idle");
#endif
                yield return PlayRecoverThenIdle_StateBased(ctx);
                yield break;
            }

            // Atk3 종료 후 Recover→Idle
            yield return PlayRecoverThenIdle_StateBased(ctx);
        }
        finally
        {
            if (bc) bc.SetMoveLocked(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 상태 해시 기반 구현
    // ─────────────────────────────────────────────────────────────────────────────

    private IEnumerator PlayStep_Atk3_WithEnterCheck_StateBased(BossContext ctx, System.Action onEnter)
    {
        int before = CurStateShortHash(ctx);

        ctx.Anims.Play(ctx.Anim, atk3Key); // CrossFade by state name
        yield return null;                 // 전이 반영

        int now = CurStateShortHash(ctx);

        // 전이가 막혔으면 1회 재시도
        if (now == before && retryEnterAtk3IfBlocked)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ComboChain] Atk3 enter blocked once → retry");
#endif
            ctx.Anims.Play(ctx.Anim, atk3Key);
            yield return null;
            now = CurStateShortHash(ctx);
        }

        // 그래도 실패
        if (now == before)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ComboChain] Atk3 did NOT start (state unchanged). " +
                             "Check AnimMapSO mapping/state name OR other code overwriting animation.");
#endif
            yield break;
        }

        onEnter?.Invoke();

        // 즉시 인터럽트 감지: 다음 프레임에 상태가 또 바뀌면 외부 덮어쓰기/AnyState 트랜지션 의심
        yield return null;
        if (CurStateShortHash(ctx) != now)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ComboChain] Atk3 interrupted immediately. " +
                             "Check AnyState/Recover/Idle/Atk2 transitions or external Play calls.");
#endif
            yield break;
        }

        // 유지되는 동안 종료 대기(타임아웃 포함)
        yield return WaitStateEnd(ctx, perStepTimeout, now);
    }

    private IEnumerator PlayStepWithDistanceBreak_StateBased(BossContext ctx, AnimKey stepKey, float limit, float timeout)
    {
        ctx.Anims.Play(ctx.Anim, stepKey);
        yield return null; // 전이 반영

        int start = CurStateShortHash(ctx);
        if (start == 0) { yield return null; start = CurStateShortHash(ctx); }

        float elapsed = 0f;
        while (start != 0 && SameState(ctx, start))
        {
            if (IsOutOfRangeSlack(ctx, limit, distanceSlack))
            {
#if UNITY_EDITOR
                Debug.Log($"[ComboChain] break to Recover by distance at {stepKey}: dx={CurDx(ctx):0.00} > {limit}+{distanceSlack}");
#endif
                ctx.Anims.Play(ctx.Anim, recoverKey);
                yield return WaitStateEnd(ctx, timeout, CurStateShortHash(ctx)); // 현재 상태 종료 대기
                ctx.Anims.Play(ctx.Anim, idleKeyAfterRecover);
                _brokeByDistance = true;
                yield break;
            }

            yield return null;
            elapsed += Time.deltaTime;
            if (timeout > 0f && elapsed >= timeout) break;
        }
    }

    private IEnumerator PlayRecoverThenIdle_StateBased(BossContext ctx)
    {
        ctx.Anims.Play(ctx.Anim, recoverKey);
        int cur = CurStateShortHash(ctx);
        yield return WaitStateEnd(ctx, perStepTimeout, cur);

        ctx.Anims.Play(ctx.Anim, idleKeyAfterRecover);
        yield return null; // 반영 프레임
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

    // ── Helpers: 상태 해시 기반 ────────────────────────────────────────────────
    private static Animator GetAnimator(BossContext ctx)
    {
        var adapter = ctx.Anim as AnimatorAdaptor;
        if (adapter != null) return adapter.UnityAnimator; // 어댑터에서 노출
        return ctx.Self ? ctx.Self.GetComponent<Animator>() : null;
    }

    private static int CurStateShortHash(BossContext ctx, int layer = 0)
    {
        var anim = GetAnimator(ctx);
        if (!anim) return 0;
        return anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
    }

    private static bool SameState(BossContext ctx, int shortHash, int layer = 0)
    {
        if (shortHash == 0) return false;
        var anim = GetAnimator(ctx);
        if (!anim) return false;
        return anim.GetCurrentAnimatorStateInfo(layer).shortNameHash == shortHash;
    }

    private static IEnumerator WaitStateEnd(BossContext ctx, float timeout, int watchedShortHash, int layer = 0)
    {
        if (watchedShortHash == 0) yield break;

        float elapsed = 0f;
        while (SameState(ctx, watchedShortHash, layer))
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
