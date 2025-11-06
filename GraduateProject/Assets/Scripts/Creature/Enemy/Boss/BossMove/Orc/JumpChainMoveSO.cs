using System.Collections;
using UnityEngine;
using static Define;

/// P1 전용: ChargeJump → JumpIn → (공중 Loop) → Land
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
    [Tooltip("ChargeJump 머무는 시간(없으면 0)")]
    public float chargeTime = 0.25f;

    [Tooltip("JumpIn 애니 한 사이클 동안 수평 이동을 금지하는 시간")]
    public float jumpInHoldTime = 0.20f;

    [Tooltip("공중 체공 시간(이 시간 동안 y축 이동 금지+중력 0)")]
    public float hoverTime = 0.35f;

    [Tooltip("Land 연출/판정 후 대기 시간")]
    public float landDelay = 0.80f;

    [Header("Jump Motion")]
    [Tooltip("수직 점프 힘(상방향)")]
    public float jumpForce = 10f;

    [Tooltip("체공 중 플레이어 쪽으로 이동하는 수평 속도")]
    public float hoverHorizontalSpeed = 2.0f;

    [Tooltip("체공 종료 후 아래로 강하게 떨어질 때의 하강 속도(절대값)")]
    public float dropForce = 16f;

    [Header("Exact Height (optional)")]
    public bool useExactJumpHeight = false;
    public float jumpHeight = 3.5f;   // 정확히 오를 높이 (월드 유닛)

    // 헬퍼
    private static float GetGravity2D(Rigidbody2D rb)
    {
        float g = Mathf.Abs(Physics2D.gravity.y);
        return g * (rb ? rb.gravityScale : 1f);
    }

    protected override IEnumerator Execute(BossContext ctx)
    {
        var bc = ctx.Self ? ctx.Self.GetComponent<BossController>() : null;
        if (ctx.RB == null || ctx.Anims == null || ctx.Anim == null) yield break;

        // 이동 덮어쓰기 방지(선택)
        if (bc) bc.SetMoveLocked(true);

        bool landed = false;
        System.Action onGround = () => landed = true;
        if (bc != null) bc.GroundTouched += onGround;

        float originalGravity = ctx.RB.gravityScale;

        // 1) Charge (옵션)
        if (chargeTime > 0f && chargeKey != AnimKey.Idle)
        {
            ctx.Anims.Play(ctx.Anim, chargeKey);
            yield return new WaitForSeconds(chargeTime);
        }

        // 목표 높이 계산
        float startY = ctx.RB.position.y;
        float targetY;
        if (useExactJumpHeight)
        {
            targetY = startY + Mathf.Max(0f, jumpHeight);
        }
        else
        {
            float g = Mathf.Abs(Physics2D.gravity.y) * ctx.RB.gravityScale;
            float estH = (jumpForce * jumpForce) / Mathf.Max(0.0001f, 2f * g);
            targetY = startY + estH;
        }

        // 2) JumpIn: X 고정 + 목표 높이로 '즉시' 스냅 (물리 스텝에서 처리)
        ctx.Anims.Play(ctx.Anim, jumpInKey);

        ctx.RB.gravityScale = 0f;
#if UNITY_6000_0_OR_NEWER
        ctx.RB.linearVelocity = Vector2.zero;
#else
    ctx.RB.velocity = Vector2.zero;
#endif
        // 리지드바디 좌표로 바로 스냅하고, 물리 스텝 동기화
        var rp = ctx.RB.position; rp.y = targetY; ctx.RB.position = rp;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // JumpIn 홀드 동안 X=0, Y 위치/속도 고정 (물리 스텝 기준)
        float held = 0f, holdDur = Mathf.Max(0.01f, jumpInHoldTime);
        while (held < holdDur)
        {
#if UNITY_6000_0_OR_NEWER
            var lv = ctx.RB.linearVelocity; lv.x = 0f; lv.y = 0f; ctx.RB.linearVelocity = lv;
#else
        var lv = ctx.RB.velocity; lv.x = 0f; lv.y = 0f; ctx.RB.velocity = lv;
#endif
            rp = ctx.RB.position; rp.y = targetY; ctx.RB.position = rp; // 드리프트 방지
            Physics2D.SyncTransforms();

            yield return new WaitForFixedUpdate();
            held += Time.fixedDeltaTime;
        }

        // 3) Loop on: 체공 — Y 완전 고정 + 중력 0, X는 천천히 플레이어 추적 (물리 스텝)
        ctx.Anims.Play(ctx.Anim, loopBoolKey, true);

        float hoverT = 0f;
        while (hoverT < hoverTime && !landed)
        {
            float dirX = 0f;
            if (ctx.Player)
            {
                float dx = ctx.Player.position.x - ctx.Self.position.x;
                dirX = Mathf.Sign(dx);
            }
#if UNITY_6000_0_OR_NEWER
            var hv = ctx.RB.linearVelocity; hv.x = dirX * hoverHorizontalSpeed; hv.y = 0f; ctx.RB.linearVelocity = hv;
#else
        var hv = ctx.RB.velocity; hv.x = dirX * hoverHorizontalSpeed; hv.y = 0f; ctx.RB.velocity = hv;
#endif
            rp = ctx.RB.position; rp.y = targetY; ctx.RB.position = rp; // Y 잠금
            Physics2D.SyncTransforms();

            yield return new WaitForFixedUpdate();
            hoverT += Time.fixedDeltaTime;
        }

        // 4) 체공 종료 → 중력 복원 + 즉시 낙하 속도 부여 (물리 스텝)
        ctx.RB.gravityScale = originalGravity;
        if (!landed)
        {
#if UNITY_6000_0_OR_NEWER
            var dv = ctx.RB.linearVelocity; dv.y = -Mathf.Abs(dropForce); ctx.RB.linearVelocity = dv;
#else
        var dv = ctx.RB.velocity; dv.y = -Mathf.Abs(dropForce); ctx.RB.velocity = dv;
#endif
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        // 5) Ground 접촉까지 대기
        while (!landed) yield return null;

        // 6) Land 1회 + 대기
        ctx.Anims.Play(ctx.Anim, landKey);
        ctx.Anims.Play(ctx.Anim, loopBoolKey, false);
        if (landDelay > 0f) yield return new WaitForSeconds(landDelay);

        // 정리
        if (bc != null) bc.GroundTouched -= onGround;
        ctx.RB.gravityScale = originalGravity;
        if (bc) bc.SetMoveLocked(false);
    }

}