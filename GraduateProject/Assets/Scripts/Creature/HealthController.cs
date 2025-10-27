using System;
using UnityEngine;

[DefaultExecutionOrder(-50)] // StatController(-100) 다음에 실행되도록 약간 빠르게
public class HealthController : MonoBehaviour, IHealth
{
    public float currentHp;
    private CombatStatSheet stats;
    private ICombatStatHolder holder;

    public float CurrentHp => currentHp;
    public CombatStatSheet Stats => stats;
    public float MaxHp => stats != null ? stats.MaxHp : 1f;

    public event Action OnDead;

    private void Awake()
    {
        // ★ 같은 GO가 아니라 부모/자식에 붙어 있을 수도 있음 → 부모까지 탐색
        ResolveStats(referenceOnly: true);
    }

    private void Start()
    {
        // ★ 순서 이슈 대비: Start에서 한 번 더 바인딩 시도
        if (stats == null) ResolveStats(referenceOnly: false);

        if (stats != null)
        {
            currentHp = stats.MaxHp;
            Debug.Log($"[HealthController] {name} 초기화 완료 MaxHp={stats.MaxHp}");
        }
        else
        {
            Debug.LogError($"[HealthController] '{name}' stats가 null 상태로 Start 호출됨. " +
                           $"holder={(holder == null ? "null" : holder.GetType().Name)}");
            currentHp = 1f; // 최소값
        }
    }

    private void ResolveStats(bool referenceOnly)
    {
        // 1) 같은 GO → 2) 부모에서 탐색
        holder = GetComponent<ICombatStatHolder>();
        if (holder == null)
            holder = GetComponentInParent<ICombatStatHolder>();

        if (holder == null) holder = GetComponentInParent<ICombatStatHolder>();
        if (holder == null) holder = GetComponentInChildren<ICombatStatHolder>(true); // ★ 추가: 자식까지 탐색

        if (holder != null)
        {
            stats = holder.Stats;
            if (stats == null)
            {
                if (!referenceOnly)
                    Debug.LogWarning($"[HealthController] '{name}' ICombatStatHolder는 찾았지만 Stats가 아직 null.");
            }
        }
        else if (!referenceOnly)
        {
            Debug.LogError($"[HealthController] '{name}' ICombatStatHolder를 찾지 못함 (부모 포함).");
        }
    }

    public void Heal(float amount)
    {
        if (stats == null) return;
        currentHp = Mathf.Min(currentHp + amount, stats.MaxHp);
    }

    public void TakeDamage(float amount)
    {
        if (stats == null) return;

        float before = currentHp;
        currentHp -= amount;
        Debug.Log($"[HP] {name}: {before:0} -> {currentHp:0} (hit {amount:0})");

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            Debug.Log($"[{gameObject.name} - DEAD] {name} 사망 처리 (OnDead Invoke)");
            OnDead?.Invoke();
        }
    }

    public void ResetHpToMax()
    {
        if (Stats != null) currentHp = Stats.MaxHp;
        else currentHp = Mathf.Max(1f, currentHp);
    }
}
