
// 스텟 제어
using System;
using UnityEngine;
using static Define;

public interface IStatHolder
{
    StatSheet Stats { get; }
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
    void OnHit(float damage, Vector2 hitDirection);
}

// 애니메이션 제어
public interface IAnimationController
{
    void Play(string clipName);
    void Stop();
}

// 공격 실행. 대상·위치·방향 등 컨텍스트 포함.
public interface IAttackBehavior
{
    
    void Execute(AttackContext context);

    float Range { get; }
    float Damage { get; }
}

// 보스 전용 인터페이스
public interface IBoss
{
    void EnterPhase(int phaseIndex);
    int CurrentPhase { get; }
}