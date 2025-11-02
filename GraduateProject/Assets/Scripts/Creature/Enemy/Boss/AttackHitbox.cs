using System.Collections.Generic;
using UnityEngine;

/// 단일 트리거 콜라이더 기반 보스/플레이어/고스트 공격 히트박스
/// - CircleCollider2D 등 Trigger 전용 권장
/// - AnimationEvent로 BeginWindow/EndWindow 호출하여 유효 프레임 제어
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Who/What to hit")]
    public LayerMask hitMask;                 // 0이면 레이어 필터 생략(IHitReactor만 있으면 적중)
    [Tooltip("윈도우 동안 같은 타겟에 중복타격 금지 간격(초)")]
    public float perTargetCooldown = 0.05f;

    [Header("Damage payload")]
    public float baseDamage = 10f;
    public Vector2 knockback = Vector2.zero;

    [Header("Debug")]
    public bool logHits = false;

    /// <summary>이 히트박스를 발동하는 주체(자기 자신 피격 방지용)</summary>
    public GameObject Source { get; set; }

    private Collider2D col;
    private Rigidbody2D rb;
    private bool windowOpen;
    private readonly Dictionary<Collider2D, float> lastHitTime = new();
    public bool useColliderEnabledAsWindow = true;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // ✅ 트리거 이벤트 보장: Rigidbody2D 필요(없으면 자동 추가)
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // 평소 닫힘(애니/코드로 열기)
        col.enabled = false;
    }

    void OnEnable() { if (useColliderEnabledAsWindow) windowOpen = true; }
    void OnDisable() { if (useColliderEnabledAsWindow) windowOpen = false; }

    /// 애니 이벤트에서 호출: 타격 유효 프레임 시작
    public void BeginWindow()
    {
        windowOpen = true;
        if (col) col.enabled = true;
        lastHitTime.Clear();
        if (logHits) Debug.Log($"[Hitbox] BeginWindow on {name}");
    }

    /// 애니 이벤트에서 호출: 타격 유효 프레임 종료
    public void EndWindow()
    {
        windowOpen = false;
        if (col) col.enabled = false;
        if (logHits) Debug.Log($"[Hitbox] EndWindow on {name}");
    }

    public void SetPayload(float damage) => baseDamage = damage;
    public void SetKnockback(float x, float y) => knockback = new Vector2(x, y);

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        // 유효 프레임 아니면 무시
        if (!windowOpen && !(useColliderEnabledAsWindow && col && col.enabled)) return;

        // 자기 자신/동일 루트 무시
        if (Source != null)
        {
            var otherRoot = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
            if (otherRoot == Source.transform) return;
        }
        else
        {
            if (other.attachedRigidbody && other.attachedRigidbody.transform == transform.root) return;
        }

        // 우선 IHitReactor 찾기(보스 허트박스 레이어가 Enemies가 아닐 수도 있음)
        var reactor = other.GetComponentInParent<IHitReactor>();
        if (reactor == null) reactor = other.GetComponentInChildren<IHitReactor>();
        if (reactor == null) return;

        // hitMask가 세팅되어 있으면 레이어 필터 통과 필요
        if (hitMask != 0)
        {
            int layer = other.gameObject.layer;
            if (((1 << layer) & hitMask.value) == 0) return;
        }

        // per-target 쿨다운
        float now = Time.time;
        if (lastHitTime.TryGetValue(other, out float t) && (now - t) < perTargetCooldown) return;
        lastHitTime[other] = now;

        // 피해 전달
        reactor.OnAttacked(baseDamage);
        if (logHits) Debug.Log($"[Hitbox] {name} -> {other.name} dmg {baseDamage}, layer={LayerMask.LayerToName(other.gameObject.layer)}");
    }
}
