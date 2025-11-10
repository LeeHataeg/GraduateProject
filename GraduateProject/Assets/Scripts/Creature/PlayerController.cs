using UnityEngine;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttackController))]
[RequireComponent(typeof(SimpleAnimationController))]
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

        if (!healthCtrl) Debug.LogError($"[{nameof(PlayerController)}] HealthController Null");
        if (!movement) Debug.LogError($"[{nameof(PlayerController)}] PlayerMovement Null");
        if (!attackCtrl) Debug.LogError($"[{nameof(PlayerController)}] PlayerAttackController Null");
        if (anim == null) Debug.LogError($"[{nameof(PlayerController)}] IAnimationController Null");
        if (!rb) Debug.LogError($"[{nameof(PlayerController)}] Rigidbody2D Null");
    }

    // ★ Start는 디버그 로그만 유지(한 번만 호출됨)
    private void Start()
    {
        Debug.Log("[PC] Start() once");
    }

    private void OnEnable()
    {
        // 구독 유실 방지: 항상 재구독(중복 방지 위해 먼저 제거 후 추가)
        if (healthCtrl != null)
        {
            healthCtrl.OnDead -= OnPlayerDead;
            healthCtrl.OnDead += OnPlayerDead;
        }
    }

    private void OnDisable()
    {
        if (healthCtrl != null)
            healthCtrl.OnDead -= OnPlayerDead;
    }

    private void OnPlayerDead()
    {
        if (isDead) return;
        isDead = true;

        if (movement) movement.enabled = false;
        if (attackCtrl) attackCtrl.enabled = false;

        if (anim != null)
        {
            anim.SetBool("isDeath", true);
            anim.SetTrigger("4_Death");
        }

#if UNITY_6000_0_OR_NEWER
        if (rb) rb.linearVelocity = Vector2.zero;
#else
        if (rb) rb.velocity = Vector2.zero;
#endif
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;

        // DeathPopup 호출
        var ui = GameManager.Instance ? GameManager.Instance.UIManager : null;
        if (ui == null)
        {
            // 씬에 있는 UIManager를 폴백으로 탐색
            ui = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        }

        if (ui != null)
            ui.ShowDeathPopup();
        else
            Debug.LogWarning("[PlayerController] UIManager가 없어 DeathPopup을 띄울 수 없습니다.");

        // Echo Runner
        if (EchoManager.I != null)
            EchoManager.I.EndBossBattle(playerDied: true);
    }

    public void Revive()
    {
        isDead = false;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = true;

        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (movement) movement.enabled = true;
        if (attackCtrl) attackCtrl.enabled = true;

        if (anim != null)
        {
            anim.SetBool("isDeath", false);
            anim.Play("Idle");
        }

        healthCtrl?.ResetHpToMax();
    }
}
