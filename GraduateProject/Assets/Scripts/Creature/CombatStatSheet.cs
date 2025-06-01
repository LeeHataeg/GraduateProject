using UnityEngine;

public class EnemyCombatStatHolder : ICombatStatHolder
{
    private CombatStatSheet stats;
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

[CreateAssetMenu(menuName = "Stats/StatSheet")]
public class CombatStatSheet : ScriptableObject
{
    public float MaxHp;
    // public float MaxMp; - MP 시스템 없을 예정
    
    // 공격력
    public float PhysAtk;
    public float MagicAtk;

    // 깡뎀과 공격 사거리, 평타 속도
    public float AtkPower;
    public float AttackRange;
    public float AtkSpeed;
    // 크리
    public float CriticalChance;
    public float CriticalDamage;

    // 회피율
    public float dodgeChance;

    // 방어력
    public float physDefense;
    public float magicDefense;

    // 흡?혈 - 뱀파이어 같은?

    // 기타 스텟 - 나중에 분리 예정
    public float DetectionRadius;
    public float MoveSpeed;

    
    public float AttackDelay;
    public float MeleeDamage;
}

[CreateAssetMenu(menuName = "Stats/PlayerStatSheet")]
public class PlayerStatSheet : CombatStatSheet
{
    


    // 기력(다크소울 처럼) - 공격과 대쉬 등에 사용되는 게이지

    // 스킬 가속 - 플레이어

    // 관통력 - 마관, 물관
    // a1 = (1 - 방깎)
    // a2 = (1 - 방관)

    // 흡?혈 - 뱀파이어 같은?
}