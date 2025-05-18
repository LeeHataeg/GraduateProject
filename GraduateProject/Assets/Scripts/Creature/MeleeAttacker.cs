using UnityEngine;
using static Define;

public class MeleeAttacker : MonoBehaviour, IAttackBehavior
{
    public float Range { get; private set; } = 1.5f;
    public float Damage { get; private set; } = 25f;

    public void Execute(AttackContext ctx)
    {
        // 1) 근접 범위 판정(Physics2D.OverlapCircle)
        // 2) 데미지 적용
        // 3) 이펙트·넉백 처리
    }
}
