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
        FindStatHolder(needLog: true);
    }

    private void Start()
    {
        if (stats == null) FindStatHolder(needLog: false);

        if (stats != null)
        {
            currentHp = stats.MaxHp;
        }
        else
        {
            Debug.LogError($"[HC] : '{name}' stats가 null");
            currentHp = 1f;
        }
    }

    private void FindStatHolder(bool needLog)
    {
        holder = GetComponent<ICombatStatHolder>();

        if (holder == null)
            holder = GetComponentInParent<ICombatStatHolder>();

        if (holder == null)
            holder = GetComponentInParent<ICombatStatHolder>();         //부모로부터 탐색
        if (holder == null) 
            holder = GetComponentInChildren<ICombatStatHolder>(true);   // 없으면 자식에서 탐색

        if (holder != null)
        {
            stats = holder.Stats;
            if (stats == null)
            {
                if (!needLog)
                    Debug.LogWarning($"[HC] '{name}' ICombatStatHolder는 있음, Stats가 아직 null.");
            }
        }
        else if (!needLog)
        {
            Debug.LogError($"[HC] '{name}' ICombatStatHolder 없음.");
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

        if (currentHp <= 0f)        // 사망
        {
            currentHp = 0f;
            OnDead?.Invoke();
        }
    }

    public void ResetHpToMax()
    {
        if (Stats != null) 
            currentHp = Stats.MaxHp;
        else 
            currentHp = Mathf.Max(1f, currentHp);
    }
}
