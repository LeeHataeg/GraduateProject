using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerStatController : MonoBehaviour
{
    [Serializable]
    public class StatTable : SerializableDictionary<Define.StatType, float> { }

    [Header("Base Stats")]
    public StatTable baseStats = new StatTable
    {
        { Define.StatType.MaxHp, 100f },
        { Define.StatType.PhysAtk, 10f },
        { Define.StatType.MagicAtk, 0f },
        { Define.StatType.Defense, 0f },
        { Define.StatType.CritChance, 0.05f },
        { Define.StatType.CritDamage, 1.5f },
        { Define.StatType.MoveSpeed, 100f },   // 너의 PlayerMovement.speed 기본값과 맞춰두면 자연스러움
        { Define.StatType.JumpForce, 3f }    // PlayerMovement.jumpForce 기본값과 동일하게
    };

    // 합산 캐시
    private readonly Dictionary<Define.StatType, float> addSum = new();
    private readonly Dictionary<Define.StatType, float> pctSum = new();

    public event Action OnStatsChanged;

    void Awake()
    {
        // 초기 0으로
        foreach (Define.StatType t in Enum.GetValues(typeof(Define.StatType)))
        {
            addSum[t] = 0f;
            pctSum[t] = 0f;
           
        }
    }

    public float Get(Define.StatType type)
    {
        float baseVal = baseStats.TryGetValue(type, out var v) ? v : 0f;
        float add = addSum[type];
        float pct = pctSum[type];
        return (baseVal + add) * (1f + pct);
    }

    public void Apply(List<Define.StatModifier> mods, int sign) // sign: +1 equip, -1 unequip
    {
        foreach (var m in mods)
        {
            if (m.op == Define.ModifierOp.Add) addSum[m.stat] += sign * m.value;
            else pctSum[m.stat] += sign * m.value;
        }
        OnStatsChanged?.Invoke();
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new();
    [SerializeField] private List<TValue> values = new();

    public void OnBeforeSerialize()
    {
        keys.Clear(); values.Clear();
        foreach (var kv in this) { keys.Add(kv.Key); values.Add(kv.Value); }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            this[keys[i]] = values[i];
    }
}