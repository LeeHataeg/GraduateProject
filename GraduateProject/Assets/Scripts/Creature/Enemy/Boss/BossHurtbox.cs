using UnityEngine;

/// 플레이어 공격 등 외부에서 OnAttacked(damage) 호출을 받는 보스 피격 리액터.
/// - 이 스크립트는 "Hurtbox" 자식 오브젝트에 붙인다.
/// - 부모의 HealthController로 피해를 전달한다.
/// - BossController의 IsInvulnerable(무적) 상태를 존중한다.
public class BossHurtbox : MonoBehaviour, IHitReactor
{
    [Header("References (Auto-Filled if null)")]
    [Tooltip("피해를 실제로 처리할 HealthController (기본: 부모에서 자동 검색)")]
    public HealthController health;

    [Tooltip("무적/상태 확인용 BossController (기본: 부모에서 자동 검색)")]
    public BossController boss;

    [Header("Damage Options")]
    [Tooltip("들어오는 최종 피해에 곱해질 배율(난이도/패턴 보정 등)")]
    public float damageMultiplier = 1f;

    [Tooltip("보스가 무적일 때(애니 이벤트 등) 피해 무시")]
    public bool respectInvulnerability = true;

    [Tooltip("디버그 로그 출력")]
    public bool debugLog = false;

    /// <summary>
    /// 맞을 때 알림(누적피격 판단용). 기존 로직에는 영향 없음.
    /// </summary>
    public System.Action<float> Damaged;

    void Awake()
    {
        if (!health) health = GetComponentInParent<HealthController>();
        if (!boss)   boss   = GetComponentInParent<BossController>();

        if (!health)
            Debug.LogError("[BossHurtbox] 부모에서 HealthController를 찾지 못했습니다.", this);
    }

    /// 외부(플레이어 히트박스 등)에서 호출하는 표준 피격 API
    public void OnAttacked(float amount)
    {
        if (!health) return;

        // 무적이면 무시
        if (respectInvulnerability && boss != null && boss.IsInvulnerable)
        {
            if (debugLog) Debug.Log("[BossHurtbox] 무적 상태로 피해 무시", this);
            return;
        }

        // 0 이하 값 무시
        if (amount <= 0f) return;

        float final = amount * Mathf.Max(0f, damageMultiplier);
        if (final <= 0f) return;

        if (debugLog) Debug.Log($"[BossHurtbox] Damage {final:0.##}", this);

        health.TakeDamage(final);

        // 누적피격 알림(옵션)
        Damaged?.Invoke(final);
    }
}
