using UnityEngine;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerHitReactor : MonoBehaviour, IHitReactor
{
    private HealthController healthCon;
    private Animator anim;
    private Rigidbody2D rigid;
    private Collider2D col;

    private bool isDead;

    private void Awake()
    {
        healthCon = GetComponent<HealthController>();
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (healthCon != null) healthCon.OnDead += OnDead;
    }

    private void OnDisable()
    {
        if (healthCon != null) healthCon.OnDead -= OnDead;
    }

    public void OnAttacked(float damage)
    {
        if (isDead) return;

        healthCon?.TakeDamage(damage);

        // 피격 애니메이션 수행
        if (anim != null)
            anim.SetTrigger("3_Damaged");
    }

    private void OnDead()
    {
        if (isDead) 
            return;

        isDead = true;

        // 1. DEATH 애니메이션 수행
        if (anim != null)
        {
            anim.SetBool("isDeath", true);
            anim.SetTrigger("4_Death");
        }

        // 2. collider off
        if (col != null)
            col.enabled = false;

        // 3. 리지드 바디  off해서 못 움직이게
        if (rigid)
        {
#if UNITY_6000_0_OR_NEWER
            rigid.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
            rigid.bodyType = RigidbodyType2D.Kinematic;
            rigid.simulated = true;
        }
    }

    // 리스폰 시
    public void Revive()
    {
        isDead = false;

        if (anim != null)
        {
            anim.ResetTrigger("4_Death");
            anim.SetBool("isDeath", false);

            // Idle 강제 시작
            TryPlay(anim, "IDLE");
            TryPlay(anim, "Idle");
        }

        if (col != null) 
            col.enabled = true;

        if (rigid)
        {
            rigid.bodyType = RigidbodyType2D.Dynamic;
            rigid.simulated = true;
#if UNITY_6000_0_OR_NEWER
            rigid.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
    }

    private static void TryPlay(Animator anim, string state)
    {
        if (anim == null) 
            return;

        try { 
            anim.Play(state, 0, 0f);
        } 
        catch { }
    }
}
