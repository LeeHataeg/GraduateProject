using UnityEngine;

[RequireComponent(typeof(IStatHolder), typeof(IHealth), typeof(IAnimationController))]
public class MonsterController : MonoBehaviour
{
    IStatHolder stats;
    IHealth health;
    IHitReactor hitReactor;
    IAnimationController anim;

    void Awake()
    {
        stats = GetComponent<IStatHolder>();
        health = GetComponent<IHealth>();
        hitReactor = GetComponent<IHitReactor>();
        anim = GetComponent<IAnimationController>();

        health.OnDead += HandleDeath;
    }

    public void TakeDamage(float dmg, Vector2 dir)
    {
        health.TakeDamage(dmg);
        hitReactor.OnAttack(dmg, dir);
        anim.Play("Hit");
    }

    void HandleDeath()
    {
        anim.Play("Death");
        // 드롭, 비활성화 등
    }
}