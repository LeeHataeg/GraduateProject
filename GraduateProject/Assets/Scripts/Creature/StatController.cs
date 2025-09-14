using UnityEngine;

public class StatController : MonoBehaviour, ICombatStatHolder
{
    [SerializeField] private CombatStatSheet stats;  // 인스펙터로 할당

    public int CurHp;

    public CombatStatSheet Stats
    {
        get
        {
            if (stats == null)
            {
                Debug.LogError($"[{nameof(StatController)}] {gameObject.name} 에 CombatStatSheet가 할당되지 않았습니다.");
            }
            return stats;
        }
    }

    private void Reset()
    {
        // 만약 인스펙터에 할당이 누락되었으면 자동으로 같은 GameObject의 Asset을 대입 시도
        if (stats == null)
        {
            stats = GetComponent<CombatStatSheet>();
        }
    }

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"[{nameof(StatController)}] Awake 시점에 stats가 null입니다. 인스펙터에서 할당해주세요.");
        }
    }

    public void ModifyHp(int modify)
    {
        CurHp += modify;
    }

    public float CalculatePhysicsDmg()
    {
        float dmg = (stats.PhysAtk + stats.BaseDmg) * Random.Range(0.9f, 1.1f);

        if (Random.value < stats.CriticalChance)
            dmg *= stats.CriticalDamage;

        return dmg;
    }

    public float CalculateMagicDmg()
    {
        float dmg = (stats.MagicAtk + stats.BaseDmg) * Random.Range(0.9f, 1.1f);

        if (Random.value < stats.CriticalChance)
            dmg *= stats.CriticalDamage;

        return dmg;
    }

    public float CalculateHybridDmg()
    {
        float dmg = ((stats.PhysAtk + stats.MagicAtk) * 0.5f + stats.BaseDmg) * Random.Range(0.9f, 1.1f);

        if (Random.value < stats.CriticalChance)
            dmg *= stats.CriticalDamage;

        return dmg;
    }
    public void Apply(System.Collections.IEnumerable modifiers, int sign) { /* 내부에서 누적/차감 후 OnStatsChanged 발생 */ }

    public void ApplyStatSheet(CombatStatSheet sheet)
    {
        if (sheet == null)
        {
            Debug.LogError($"[StatController] Null sheet on {name}");
            return;
        }
        // private [SerializeField]이므로 this.stats에 대입만 해주면 런타임 반영됨
        // 만약 이 아래에서 CurHp 등 현재 HP를 세팅하려면 sheet.MaxHp로 초기화 가능
        var f = typeof(StatController).GetField("stats", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f != null) f.SetValue(this, sheet);
        else Debug.LogWarning("[StatController] Could not set stats via reflection (field name changed?)");
    }

}
