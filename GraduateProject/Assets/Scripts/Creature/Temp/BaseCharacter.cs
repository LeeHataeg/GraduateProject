using System;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class BaseCharacter : MonoBehaviour, IHealth, IHitReactor/*, IAnimationController*/
{
    [SerializeField] BaseStat stats;
    public BaseStat Stats => stats;

    public float CurrentHp { get; private set; }
    public event Action OnDead;

    Animator animator;
    Rigidbody2D rb;
    bool isDead = false;

    void Awake()
    {
        CurrentHp = stats.MaxHp;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        Debug.Log($"[BaseCharacter] anim={animator}, rb={rb}, stat={stats}, ¡¦");
    }

    // IHealth
    public void TakeDamage(float amount)
    {
        float net = Mathf.Max(amount - stats.Defense, 1f);
        OnAttack(net, Vector2.zero);
    }
    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, stats.MaxHp);
    }

    // IHitReactor
    public void OnAttack(float damage, Vector2 hitDirection)
    {
        if (isDead) return;

        CurrentHp -= damage;
        Play("Hit");

        if (hitDirection != Vector2.zero)
            rb.AddForce(hitDirection.normalized * damage * 50f, ForceMode2D.Impulse);

        if (CurrentHp <= 0f)
        {
            isDead = true;
            Play("Die");
            OnDead?.Invoke();
        }
    }

    // IAnimationController
    public void Play(string clipName)
    {
        if (animator) animator.Play(clipName);
    }
    public void Stop()
    {
        if (animator) animator.StopPlayback();
    }
}
