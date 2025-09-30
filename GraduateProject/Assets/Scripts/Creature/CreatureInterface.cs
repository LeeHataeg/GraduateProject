using System;
using UnityEngine;
using static Define;

//public struct AttackContext
//{
//    public Vector2 Origin;            // 공격 시작 위치
//    public Vector2 Direction;         // 공격 방향 (Normalized)
//    public GameObject Source;   // 공격 주체
//    public float Damage;  // 실제 사용된 공격 동작
//}

// 스텟 제어
public interface ICombatStatHolder
{
    CombatStatSheet Stats { get; }

    void ModifyHp(int modify);

    public float CalculatePhysicsDmg();
    public float CalculateMagicDmg();
    public float CalculateHybridDmg();
}

// 체력 제어
public interface IHealth
{
    float CurrentHp { get; }
    void TakeDamage(float amount);
    void Heal(float amount);
    event Action OnDead;
}

// 피격 제어
public interface IHitReactor
{
    void OnAttacked(float damage);
}

// 애니메이션 제어
public interface IAnimationController
{
    void Play(string clipName);
    void Stop();
    void SetBool(string paramName, bool value);
    void SetTrigger(string paramName);
    public void SetFloat(string name, float value);
    public void SetInt(string name, int value);
    public string GetCurClipname();
}

// 공격 실행. 대상·위치·방향 등 컨텍스트 포함.
public interface IAttackBehavior
{
    float Range { get; }
    void Execute(Vector2 position, float dmg, float atkRange);
}

// 보스 전용 인터페이스
public interface IBoss
{
    void EnterPhase(int phaseIndex);
    int CurrentPhase { get; }
}