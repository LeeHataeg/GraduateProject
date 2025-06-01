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
        int dmg = (int)((stats.AtkPower) * stats.CriticalDamage * stats.PhysAtk);
        return dmg;
    }
}
