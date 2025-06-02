using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// �÷��̾ ������ �޾��� �� ü�� ����, �ǰ� �ִϸ��̼�, �˹�, ���� �� ó���� ����մϴ�.
/// </summary>
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(IAnimationController))]
public class PlayerHitReactor : MonoBehaviour, IHitReactor
{
    private HealthController healthCtrl;
    private Rigidbody2D rb;
    private IAnimationController anim;
    private Collider2D col;

    [Header("�ǰ� ����")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float invincibleDuration = 0.5f;

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

    /// <summary>
    /// �ܺο��� �÷��̾ ���ݹ��� �� ȣ��˴ϴ�.
    /// </summary>
    /// <param name="damage">���� ������</param>
    /// <param name="hitDirection">�˹� ���� (�� ��ġ���� �÷��̾� ��ġ�� �� ����)</param>
    public void OnAttack(float damage, Vector2 hitDirection)
    {
        if (isDead || isInvincible) return;

        // (1) ���� ó��
        StartCoroutine(InvincibleCoroutine());

        // (2) ü�� ����
        healthCtrl.TakeDamage(damage);

        // (3) �ǰ� �ִϸ��̼� ���
        anim.SetTrigger("3_Damaged");

        // (4) �˹�
        Vector2 kbDir = hitDirection.normalized;
        rb.AddForce(kbDir * knockbackForce, ForceMode2D.Impulse);
    }

    private IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        // ���Ѵٸ� ��������Ʈ ������ ó�� ���� ���⿡ �߰�
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    private void OnDeadHandler()
    {
        if (isDead) return;
        isDead = true;

        // (1) ��� �ִϸ��̼�
        anim.SetBool("isDeath", true);
        anim.SetTrigger("4_Death");

        // (2) �ݶ��̴��� ���� ��Ȱ��ȭ
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // (3) �ʿ��� ��� ������ �Ǵ� ���ӿ��� ���� ȣ��
        // ����: GameOverManager.Instance.TriggerGameOver();

        // (4) �÷��̾� ������Ʈ�� �ٷ� �ı����� �ʰ�, ��� �ִϸ��̼��� ���� �� ó���ϵ��� �ڷ�ƾ ��� ����
    }
}
