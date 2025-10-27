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

        if (healthCtrl == null)
            Debug.LogError($"[{nameof(PlayerController)}] HealthController Null");
        if (movement == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerMovement Null");
        if (attackCtrl == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerAttackController Null");
        if (anim == null)
            Debug.LogError($"[{nameof(PlayerController)}] IAnimationController Null");
        if (rb == null)
            Debug.LogError($"[{nameof(PlayerController)}] Rigidbody2D Null");
    }

    private void Start()
    {
        healthCtrl.OnDead += OnPlayerDead;
    }

    private void OnPlayerDead()
    {
        if (isDead) return;
        isDead = true;

        movement.enabled = false;
        attackCtrl.enabled = false;

        anim.SetBool("isDeath", true);
        anim.SetTrigger("4_Death");

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif

        rb.bodyType = RigidbodyType2D.Kinematic;

        var ui = GameManager.Instance != null ? GameManager.Instance.UIManager : null;
        if (ui != null)
        {
            ui.ShowDeathPopup();
        }
        else
        {
            Debug.Log("ziral");
            Debug.LogWarning("[PlayerController] UIManager가 없어 DeathPopup을 띄울 수 없습니다.");
        }

        // === [ADD] Echo Runner: 플레이어 사망 처리 ===
        if (EchoManager.I != null)
        {
            EchoManager.I.EndBossBattle(playerDied: true);
        }
    }

    // PlayerController.cs
    public void Revive()
    {
        isDead = false;

        // 컨트롤/충돌 복구
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

        // 애니 상태 복구
        if (anim != null)
        {
            anim.SetBool("isDeath", false);
            // (선택) 트리거 초기화 유틸이 있다면 써주고, 없다면 Idle로 Play
            anim.Play("Idle");
        }

        // HP 만땅
        if (healthCtrl != null)
        {
            // 안전하게 HP를 최대치로
            //typeof(HealthController).GetField("currentHp",
            //    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            //    ?.SetValue(healthCtrl, healthCtrl.MaxHp);

            healthCtrl.ResetHpToMax();
        }

        //if (hitReactor != null)
        //{
        //    hitReactor.SetTemporaryInvincible(0.2f); // 구현되어 있다면
        //}
    }

}
