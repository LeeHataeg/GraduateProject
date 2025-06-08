using UnityEngine;

public class EnemyCombatStatHolder : MonoBehaviour, ICombatStatHolder
{
    [SerializeField] private CombatStatSheet stats;
    public CombatStatSheet Stats => stats;

    public int curHp;

    public void ModifyHp(int modify)
    {
        curHp += modify;
    }

    // 아이템은 어캐 적용할거임?
    public int GetMeleeDmg()
    {
        // 차후 계수를 추가하게 될 지도
        int dmg = (int)((stats.BaseDmg) * stats.CriticalDamage * stats.PhysAtk);
        return dmg;
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
}
