using UnityEngine;

[CreateAssetMenu(menuName = "Stats/StatSheet")]
public class StatSheet : ScriptableObject
{
    public float MaxHp;
    // public float MaxMp; - MP 시스템 없을 예정
    
    // 공격력
    public float PhysAtk;
    public float MagicAtk;

    // 실질적 최종뎀과 공격 사거리, 평타 속도
    public float AtkPower;
    public float AtkRange;
    public float AtkSpeed;

    // 크리
    public float CriticalChance;
    public float CriticalDamage;

    // 회피율
    public float dodgeChance;

    public float totalDefense;

    // 흡?혈 - 뱀파이어 같은?
}

[CreateAssetMenu(menuName = "Stats/PlayerStatSheet")]
public class PlayerStatSheet : StatSheet
{
    // 방어력
    public float physDefense; 
    public float magicDefense;

    // 기력(다크소울 처럼) - 공격과 대쉬 등에 사용되는 게이지

    // 스킬 가속 - 플레이어

    // 관통력 - 마관, 물관
    // a1 = (1 - 방깎)
    // a2 = (1 - 방관)

    // 흡?혈 - 뱀파이어 같은?
}