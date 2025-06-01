using System;
using UnityEngine;
using static Define;

public struct AttackContext
{
    public Vector2 Origin;            // 공격 시작 위치
    public Vector2 Direction;         // 공격 방향 (Normalized)
    public GameObject Source;   // 공격 주체
    public float Damage;  // 실제 사용된 공격 동작
}

// Enemy 내부에 원 범위의(Sprite는 추가하지 않을 예정) 오브젝트에
//  아래 스크립트를 달아서 범위 안에 들어옴-> 공격 사거리까지 추격
public class PlayerDetector : MonoBehaviour
{
    public AttackContext AttackContext;

    private void OnTriggerStay2D(Collider2D coll)
    {
        if (coll.CompareTag("Player"))
        {
            // step 01. 접근

            // step 02. range 내로 들어왔다면 공격
            //      range 기준은 오브젝트 크기일텐데... 변동성 고려하면 너무 위험한뎅?

        }
    }
}

// 스텟 제어
public interface ICombatStatHolder
{
    CombatStatSheet Stats { get; }

    void ModifyHp(int modify);
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
    void OnAttack(float damage, Vector2 hitDirection);
}

// 애니메이션 제어
public interface IAnimationController
{
    void Play(string clipName);
    void Stop();
    void SetBool(string paramName, bool value);
    void SetTrigger(string paramName);
}

// 공격 실행. 대상·위치·방향 등 컨텍스트 포함.
public interface IAttackBehavior
{
    float Range { get; }
    void Execute(AttackContext context);
}

// 보스 전용 인터페이스
public interface IBoss
{
    void EnterPhase(int phaseIndex);
    int CurrentPhase { get; }
}