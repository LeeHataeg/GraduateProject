using System.Collections.Generic;
using UnityEngine;

/// 단일 트리거 콜라이더 기반 보스 공격 히트박스
/// - Collider2D는 애니메이션으로 size/offset을 키프레임
/// - AnimationEvent로 BeginWindow/EndWindow 호출하여 유효 프레임 제어
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Who/What to hit")]
    public LayerMask hitMask;
    [Tooltip("윈도우 동안 같은 타겟에 중복타격 금지 간격(초)")]
    public float perTargetCooldown = 0.05f;

    [Header("Damage payload")]
    public float baseDamage = 10f;
    public Vector2 knockback = Vector2.zero;

    [Header("Debug")]
    public bool logHits = false;

    private Collider2D col;
    private bool windowOpen;
    private readonly Dictionary<Collider2D, float> lastHitTime = new();

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;      // 평소 비활성(권장)
    }

    /// 애니 이벤트에서 호출: 타격 유효 프레임 시작
    public void BeginWindow()
    {
        windowOpen = true;
        col.enabled = true;
        lastHitTime.Clear(); // 윈도우별 중복히트 초기화
    }

    /// 애니 이벤트에서 호출: 타격 유효 프레임 종료
    public void EndWindow()
    {
        windowOpen = false;
        col.enabled = false;
    }

    /// 필요 시 애니 이벤트로 공격력/넉백 덮어쓰기
    public void SetPayload(float damage) => baseDamage = damage;
    public void SetKnockback(float x, float y) => knockback = new Vector2(x, y);

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 프레임 내 콜라이더 크기 변경 시 Enter가 안 뜰 수도 있으므로 보조로 Stay에서도 시도
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!windowOpen) return;
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;
        if (other.attachedRigidbody && other.attachedRigidbody.transform == transform.root) return;

        float now = Time.time;
        if (lastHitTime.TryGetValue(other, out float t) && (now - t) < perTargetCooldown) return;

        lastHitTime[other] = now;

        // 피해 전달
        var hr = other.GetComponent<IHitReactor>();
        if (hr != null)
        {
            hr.OnAttacked(baseDamage);
            if (logHits) Debug.Log($"[Hitbox] {name} -> {other.name} dmg {baseDamage}");
        }

        // 넉백 예시(상대에 Rigidbody2D가 있고 원하면)
        var rb = other.attachedRigidbody;
        if (rb)
        {
            var v = rb.linearVelocity;
            v += knockback;
            rb.linearVelocity = v;
        }
    }
}
