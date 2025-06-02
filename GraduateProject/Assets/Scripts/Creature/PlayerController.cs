using UnityEngine;

/// <summary>
/// �÷��̾� ��ü �帧(��� �� �Է�/�̵�/���� ����)�� �Ѱ��մϴ�.
/// </summary>
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttackController))]
[RequireComponent(typeof(IAnimationController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private HealthController healthCtrl;
    private PlayerMovement movement;
    private PlayerAttackController attackCtrl;
    private IAnimationController anim;
    private Rigidbody2D rb;
    private bool isDead = false;

    private void Awake()
    {
        healthCtrl = GetComponent<HealthController>();
        movement = GetComponent<PlayerMovement>();
        attackCtrl = GetComponent<PlayerAttackController>();
        anim = GetComponent<IAnimationController>();
        rb = GetComponent<Rigidbody2D>();

        if (healthCtrl == null)
            Debug.LogError($"[{nameof(PlayerController)}] HealthController�� �����ϴ�.");
        if (movement == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerMovement�� �����ϴ�.");
        if (attackCtrl == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerAttackController�� �����ϴ�.");
        if (anim == null)
            Debug.LogError($"[{nameof(PlayerController)}] IAnimationController�� �����ϴ�.");
        if (rb == null)
            Debug.LogError($"[{nameof(PlayerController)}] Rigidbody2D�� �����ϴ�.");
    }

    private void Start()
    {
        healthCtrl.OnDead += OnPlayerDead;
    }

    private void OnPlayerDead()
    {
        if (isDead) return;
        isDead = true;

        // (1) �̵�/���� ��� ����
        movement.enabled = false;
        attackCtrl.enabled = false;

        // (2) ��� �ִϸ��̼� ���
        anim.SetBool("isDeath", true);
        anim.Play("4_Death");

        // (3) ���� ��Ȱ��ȭ
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // (4) ���� ���� ȭ�� ȣ�� �� �߰� ����
        // Example: GameOverManager.Instance.ShowGameOver();
    }
}
