using UnityEngine;

/// <summary>
/// 플레이어 피격/사망 반응. EnemyHitReactor와 동일 컨셉.
/// Animator 파라미터: isDeath(bool), 3_Damaged(trigger), 4_Death(trigger)
/// </summary>
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerHitReactor : MonoBehaviour, IHitReactor
{
    private HealthController _hp;
    private Animator _anim;
    private Rigidbody2D _rb;
    private Collider2D _col;

    private bool _isDead;

    private void Awake()
    {
        _hp = GetComponent<HealthController>();
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (_hp != null) _hp.OnDead += OnDeadHandler;
    }

    private void OnDisable()
    {
        if (_hp != null) _hp.OnDead -= OnDeadHandler;
    }

    public void OnAttacked(float damage)
    {
        if (_isDead) return;

        _hp?.TakeDamage(damage);

        // 피격 연출
        if (_anim != null)
            _anim.SetTrigger("3_Damaged");
    }

    private void OnDeadHandler()
    {
        if (_isDead) return;
        _isDead = true;

        if (_anim != null)
        {
            _anim.SetBool("isDeath", true);
            _anim.SetTrigger("4_Death");
        }

        if (_col) _col.enabled = false;

        if (_rb)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = true;
        }
    }

    /// <summary>
    /// Restart/부활 시 호출: 사망 상태/컴포넌트/애니메이터를 정상화
    /// </summary>
    public void ClearDeadFlag()
    {
        _isDead = false;

        if (_anim != null)
        {
            _anim.ResetTrigger("4_Death");
            _anim.SetBool("isDeath", false);
            // 필요하면 Idle로 강제 점프
            TryPlay(_anim, "IDLE");
            TryPlay(_anim, "Idle");
        }

        if (_col) _col.enabled = true;

        if (_rb)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.simulated = true;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
    }

    private static void TryPlay(Animator a, string state)
    {
        if (!a) return;
        try { a.Play(state, 0, 0f); } catch { }
    }
}
