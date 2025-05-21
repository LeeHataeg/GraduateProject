using UnityEngine;

[System.Serializable]
public class BaseStat : MonoBehaviour
{
    [Header("Core Stats")]
    public float MaxHp = 100f;
    public float Attack = 20f;
    public float Defense = 5f;
}

public struct AttackContext
{
    public Vector2 Origin;            // 공격 시작 위치
    public Vector2 Direction;         // 공격 방향 (Normalized)
    //public IStatHolder Attacker;      // 공격자 스탯 정보
    //public IAttackBehavior Behavior;  // 실제 사용된 공격 동작
    public BaseStat Attacker;      // 공격자 스탯 정보
    public MeleeAttackBehavior Behavior;  // 실제 사용된 공격 동작
}