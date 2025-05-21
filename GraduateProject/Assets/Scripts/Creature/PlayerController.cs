using UnityEngine;

[RequireComponent(typeof(IStatHolder), typeof(IHealth), typeof(IAnimationController))]
public class PlayerController : MonoBehaviour
{
    IStatHolder stats;
    IHealth health;
    IHitReactor hitReactor;
    IAnimationController anim;
    IAttackBehavior attacker;

    void Awake()
    {
        stats = GetComponent<IStatHolder>();
        health = GetComponent<IHealth>();
        hitReactor = GetComponent<IHitReactor>();
        anim = GetComponent<IAnimationController>();
        attacker = GetComponent<IAttackBehavior>();
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space)) attacker.Attack();
    }

    public void TakeDamage(float dmg, Vector2 dir)
    {
        health.TakeDamage(dmg);
        hitReactor.OnAttack(dmg, dir);
        anim.Play("Hit");
    }
}
