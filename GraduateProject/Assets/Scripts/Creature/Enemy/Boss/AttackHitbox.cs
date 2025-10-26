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
    public bool useColliderEnabledAsWindow = true;
    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;      // 평소 비활성(권장)
    }

    void OnEnable()
    {
        if (useColliderEnabledAsWindow) windowOpen = true;
    }
    void OnDisable()
    {
        if (useColliderEnabledAsWindow) windowOpen = false;
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
        Debug.Log($"[Hitbox] Enter with {other.name}, tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)}");
        //if (other.tag == "Player")
        //{
        //    Debug.Log("UnitRoot 탐지");
        //    var plHit = other.GetComponent<IHitReactor>();
        //    if(plHit != null)
        //    {
        //        Debug.Log("plHit 탐지");
        //        plHit.OnAttacked(baseDamage);
        //    }
        //}


        //if (LayerMask.NameToLayer("Player") == other.gameObject.layer){
        //    Debug.Log("아~싸 레이어 탐지 개꿀이고");
        //    var plHit = other.GetComponent<IHitReactor>();
        //    if (plHit != null)
        //    {
        //        Debug.Log("plHit 탐지");
        //        plHit.OnAttacked(baseDamage);
        //    }
        //}
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 프레임 내 콜라이더 크기 변경 시 Enter가 안 뜰 수도 있으므로 보조로 Stay에서도 시도
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!windowOpen && !(useColliderEnabledAsWindow && col && col.enabled)) return;

        if (((1 << other.gameObject.layer) & hitMask) == 0) return;
        if (other.attachedRigidbody && other.attachedRigidbody.transform == transform.root) return;

        float now = Time.time;
        if (lastHitTime.TryGetValue(other, out float t) && (now - t) < perTargetCooldown) return;

        lastHitTime[other] = now;

        // 피해 전달
        var hr = other.GetComponentInParent<IHitReactor>();
        if (hr == null) hr = other.GetComponentInChildren<IHitReactor>();
        if (hr != null)
        {
            hr.OnAttacked(baseDamage);
            if (logHits) Debug.Log($"[Hitbox] {name} -> {other.name} dmg {baseDamage}");
        }
    }
}
