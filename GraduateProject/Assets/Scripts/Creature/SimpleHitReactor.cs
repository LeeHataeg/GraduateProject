using UnityEngine;

public class SimpleHitReactor : MonoBehaviour, IHitReactor
{
    private HealthController healthCtrl;
    private Rigidbody2D rb;
    private IAnimationController anim;
    private Collider2D col;

    [SerializeField] private float knockbackForce = 5f;

    private bool isDead = false;

    private void Awake()
    {
        healthCtrl = GetComponent<HealthController>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<IAnimationController>();
        col = GetComponent<Collider2D>();

        if (healthCtrl == null || rb == null || anim == null || col == null)
        {
            Debug.LogError($"[{nameof(SimpleHitReactor)}] 필수 컴포넌트가 누락되었습니다.");
        }
    }

    private void Start()
    {
        healthCtrl.OnDead += OnDeadHandler;
    }

    public void OnAttack(float damage, Vector2 hitDirection)
    {
        if (isDead) return;

        // (1) 체력 차감
        healthCtrl.TakeDamage(damage);

        // (2) 피격 애니메이션 재생
        anim.Play("3_Damaged");

        // (3) 넉백
        Vector2 knockbackDir = hitDirection.normalized;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
    }

    private void OnDeadHandler()
    {
        if (isDead) return;
        isDead = true;

        // (1) 피격 반응이 끝난 뒤 사망 애니메이션 재생
        anim.SetBool("isDeath", true);
        anim.Play("4_Death");

        // (2) Collider/Rigidbody 비활성화
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // (3) 사망 후 일정 시간 뒤 오브젝트 제거
        Destroy(gameObject, 1.5f); // 애니메이션 길이에 맞춰 조정
    }
}
