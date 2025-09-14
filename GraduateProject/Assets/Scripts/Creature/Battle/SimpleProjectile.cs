using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class SimpleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 dir;
    private float speed;
    private float lifeTime;
    private float damage;
    private LayerMask hitMask;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // 권장
    }

    public void Init(Vector2 direction, float dmg, GameObject owner, LayerMask hitMask, float speed, float lifeTime)
    {
        this.dir = direction.normalized;
        this.damage = dmg;
        this.owner = owner;
        this.hitMask = hitMask;
        this.speed = speed;
        this.lifeTime = lifeTime;
    }

    private void Start()
    {
        SetVel(dir * speed);
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleHit(other);

    private void HandleHit(Collider2D other)
    {
        if (!other || !other.gameObject) return;

        // 아군/자기 자신 무시
        if (owner && other.transform.IsChildOf(owner.transform)) return;

        // 레이어 필터
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var reactor = other.GetComponent<IHitReactor>();
        if (reactor != null)
        {
            reactor.OnAttacked(damage);
            Destroy(gameObject);
        }
    }

    // -------- velocity/linearVelocity 호환 유틸 --------
    private Vector2 GetVel()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVel(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}
