using System;
using UnityEngine;

public class HealthController : MonoBehaviour, IHealth
{
    private float currentHp;
    private CombatStatSheet stats;

    public float CurrentHp => currentHp;
    public CombatStatSheet Stats => stats;
    public float MaxHp => stats != null ? stats.MaxHp : 1f;

    public event Action OnDead;

    private void Awake()
    {
        // 동일 GameObject에 있는 ICombatStatHolder(StatController 등)에서 스탯 가져오기
        var statHolder = GetComponent<ICombatStatHolder>();
        if (statHolder == null)
        {
            Debug.LogError($"[{nameof(HealthController)}] ICombatStatHolder를 찾을 수 없습니다.");
        }
        else
        {
            stats = statHolder.Stats;
        }
    }

    private void Start()
    {
        if (stats != null)
        {
            currentHp = stats.MaxHp;
        }
        else
        {
            Debug.LogError($"[{nameof(HealthController)}] stats가 null 상태로 Start가 호출되었습니다.");
            currentHp = 1f; // 최소값 대입
        }
    }

    public void Heal(float amount)
    {
        if (stats == null) return;
        currentHp = Mathf.Min(currentHp + amount, stats.MaxHp);
    }

    // HealthController.cs - TakeDamage 내부
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

}
