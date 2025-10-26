using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(IAnimationController))]
public class PlayerHitReactor : MonoBehaviour, IHitReactor
{
    private HealthController healthCtrl;
    private Rigidbody2D rb;
    private IAnimationController anim;
    private Collider2D col;

    private bool isDead = false;
    private bool isInvincible = false;

    private void Awake()
    {
        healthCtrl = GetComponent<HealthController>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<IAnimationController>();
        col = GetComponent<Collider2D>();

        if (healthCtrl == null)
            Debug.LogError($"[{nameof(PlayerHitReactor)}] HealthController�� ���� ������ ó���� �� �� �����ϴ�.");
        if (rb == null)
            Debug.LogError($"[{nameof(PlayerHitReactor)}] Rigidbody2D�� ���� �˹� ó���� �� �� �����ϴ�.");
        if (anim == null)
            Debug.LogError($"[{nameof(PlayerHitReactor)}] IAnimationController�� ���� �ִϸ��̼��� ����� �� �����ϴ�.");
        if (col == null)
            Debug.LogError($"[{nameof(PlayerHitReactor)}] Collider2D�� ���� �浹 ó���� �� �� �����ϴ�.");
    }

    private void Start()
    {
        healthCtrl.OnDead += OnDeadHandler;
    }

    public void OnAttacked(float damage)
    {
        if (isDead || isInvincible) return;

        // 데미지 감산
        healthCtrl.TakeDamage(damage);

        // 애니메이션 작동
        anim.SetTrigger("3_Damaged");
    }

    private void OnDeadHandler()
    {
        if (isDead) return;
        isDead = true;

        // 애니메이션 작동
        anim.SetBool("isDeath", true);
        anim.SetTrigger("4_Death");

        //  기능 정지
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 게임오버 UI
        GameManager.Instance.UIManager.ShowDeathPopup();
    }
}
