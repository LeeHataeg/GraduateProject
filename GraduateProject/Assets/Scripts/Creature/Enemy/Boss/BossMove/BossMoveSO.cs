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

    // 선택 조건들(예: 사거리, 점프필요, 지상 필요 등)이 있다면 여기에 추가
    [Header("Gating (Optional)")]
    public float minRange = 0f;
    public float maxRange = 999f;
    public bool requireGrounded = false;

    private float _nextReadyTime;

    public virtual bool CanRun(BossContext ctx)
    {
        // 쿨다운
        if (Time.time < _nextReadyTime)
        {
            Debug.Log($"[Move:{name} Move SO] CD"); // ← 사용자가 본 로그
            return false;
        }

        // 사거리
        if (ctx.Player)
        {
            float dx = Mathf.Abs(ctx.Player.position.x - ctx.Self.position.x);
            if (dx < minRange || dx > maxRange)
            {
                // Debug.Log($"[Move:{name}] Range block ({dx:0.0})");
                return false;
            }
        }

        // 지상 필요 조건
        if (requireGrounded)
        {
            // Ground 체크를 별도 컨트롤러에서 받아온다면 그 값을 쓰면 됨.
            // 여기선 간단히 "수직속도 거의 0" 정도로 판정(임시)
#if UNITY_6000_0_OR_NEWER
            if (Mathf.Abs(ctx.RB.linearVelocity.y) > 0.05f) return false;
#else
            if (Mathf.Abs(ctx.RB.velocity.y) > 0.05f) return false;
#endif
        }

        return true;
    }

    public IEnumerator Run(BossContext ctx)
    {
        _nextReadyTime = Time.time + cooldown;

        // 걷기 루프 강제 중지(Animator Bool: Walking=false)
        ctx.Anims.Play(ctx.Anim, AnimKey.Walking, false);

        if (lockMovement) ctx.LockMove(true);

        // 공격 애니 트리거
        ctx.Anims.Play(ctx.Anim, animKey);

        if (windup > 0) yield return new WaitForSeconds(windup);

        yield return Execute(ctx);

        if (recover > 0) yield return new WaitForSeconds(recover);

        if (lockMovement) ctx.LockMove(false);

        // 공격이 끝났다고 곧바로 Walking=true를 다시 켜지 말고
        // 이동 여부는 컨트롤러(Update)에서 속도로 판단해 토글하도록 맡긴다.
    }

    protected abstract IEnumerator Execute(BossContext ctx);
}
