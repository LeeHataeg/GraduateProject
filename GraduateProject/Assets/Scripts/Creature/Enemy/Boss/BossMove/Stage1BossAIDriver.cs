using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Stage1BossAIDriver : MonoBehaviour
{
    [Header("Refs")]
    public BossAIProfileSO profile;
    public Transform player;                // 비우면 자동 탐색(Tag=Player)
    public Animator anim;                   // 비우면 자동 탐색(자식)
    public Rigidbody2D rb;                  // 선택(점프 중 속도 제어용)
    public LayerMask groundMaskOverride;    // 비우면 profile.groundMask 사용

    [Header("Animator Triggers (Stage1 전용)")]
    public string TR_ToJump = "ToJump";
    public string TR_LightAtk = "LightAtk";
    public string TR_FrontHeavy = "FrontHeavyAtk";

    [Header("State")]
    public bool debugLog;
    private float lastJumpT, lastLightT, lastHeavyT;
    private bool grounded;

    private float senseTimer;

    private void Reset()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private IEnumerator Start()
    {
        if (!profile)
        {
            Debug.LogError("[Stage1BossAIDriver] Profile is null.", this);
            enabled = false; yield break;
        }
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();

        // 센싱 루프
        while (true)
        {
            TickSenseAndDecide();
            float dt = Mathf.Max(0.02f, profile.senseInterval);
            yield return new WaitForSeconds(dt);
        }
    }

    private void TickSenseAndDecide()
    {
        if (!player || !anim) return;

        // --- 센싱 ---
        Vector2 pos = transform.position;
        Vector2 ppos = player.position;
        float dx = ppos.x - pos.x;
        float dy = ppos.y - pos.y;
        float ax = Mathf.Abs(dx);
        float ay = Mathf.Abs(dy);
        float now = Time.time;

        grounded = ProbeGrounded(pos);

        // 페이스 방향 보정(선택)
        if (Mathf.Abs(dx) > profile.faceFlipThreshold)
        {
            Vector3 sc = transform.localScale;
            sc.x = Mathf.Sign(dx) * Mathf.Abs(sc.x);
            transform.localScale = sc;
        }

        // --- 우선: 점프 조건 ---
        bool wantJump =
            grounded &&
            ay >= profile.jumpYThreshold &&
            ax >= profile.jumpMinX && ax <= profile.jumpMaxX &&
            (now - lastJumpT) >= profile.jumpCooldown;

        // --- 공격 후보 ---
        bool inLight =
            ax >= profile.lightMinX && ax <= profile.lightMaxX &&
            ay <= profile.lightMaxAbsY &&
            (now - lastLightT) >= profile.lightCooldown;

        bool inHeavy =
            ax >= profile.heavyMinX && ax <= profile.heavyMaxX &&
            ay <= profile.heavyMaxAbsY &&
            (now - lastHeavyT) >= profile.heavyCooldown &&
            HasFrontLineOfSight(pos, dx);

        // 의사결정: Jump > 공격(가중치) > 대기
        if (wantJump)
        {
            TriggerOnce(TR_ToJump);
            lastJumpT = now;
            if (debugLog) Debug.Log("[Stage1BossAIDriver] JUMP", this);
            return;
        }

        if (inLight || inHeavy)
        {
            // 둘 다 되면 preferHeavy 기반으로 선택
            if (inLight && inHeavy)
            {
                if (Random.value < profile.preferHeavy) FireHeavy();
                else FireLight();
            }
            else if (inHeavy) FireHeavy();
            else FireLight();

            return;
        }

        // 그 외: 아무 것도 안 함(Idle 유지)
    }

    private void FireLight()
    {
        TriggerOnce(TR_LightAtk);
        lastLightT = Time.time;
        if (debugLog) Debug.Log("[Stage1BossAIDriver] LightAtk", this);
    }

    private void FireHeavy()
    {
        TriggerOnce(TR_FrontHeavy);
        lastHeavyT = Time.time;
        if (debugLog) Debug.Log("[Stage1BossAIDriver] FrontHeavyAtk", this);
    }

    private void TriggerOnce(string trig)
    {
        if (string.IsNullOrEmpty(trig) || !anim) return;
        // 같은 프레임 중복 방지 → 재설정
        anim.ResetTrigger(trig);
        anim.SetTrigger(trig);
    }

    private bool ProbeGrounded(Vector2 from)
    {
        var mask = (groundMaskOverride.value != 0) ? groundMaskOverride : profile.groundMask;
        // 발 아래 소폭 레이
        RaycastHit2D hit = Physics2D.Raycast(from + Vector2.down * 0.05f, Vector2.down, 0.2f, mask);
        return hit.collider != null;
    }

    private bool HasFrontLineOfSight(Vector2 from, float dx)
    {
        // 전방으로만 가시선 체크(벽 사이에 플레이어가 있으면 Heavy를 억제)
        var dir = new Vector2(Mathf.Sign(dx), 0f);
        float len = profile.losRayLength;
        var mask = (groundMaskOverride.value != 0) ? groundMaskOverride : profile.groundMask;
        var hit = Physics2D.Raycast(from + Vector2.up * 0.5f, dir, len, mask);
        // 벽에 먼저 막히지 않으면 true
        return hit.collider == null;
    }
}
