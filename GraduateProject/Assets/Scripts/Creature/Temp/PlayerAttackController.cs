using UnityEngine;

[RequireComponent(typeof(MeleeAttackBehavior))]
public class PlayerAttackController : MonoBehaviour
{
    MeleeAttackBehavior attackBehavior;

    void Awake()
    {
        attackBehavior = GetComponent<MeleeAttackBehavior>();
    }

    public void Hit()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var ctx = new AttackContext
        {
            Origin = transform.position,
            Direction = dir,
            Attacker = GetComponent<BaseStat>(),
            Behavior = attackBehavior
        };
        attackBehavior.Execute(ctx);
    }
}