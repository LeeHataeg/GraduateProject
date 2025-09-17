using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class SimpleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;

    // 상태
    private GameObject owner;
    private float damage;
    private LayerMask hitMask;
    private bool fired;

    [Header("Defaults (Init 미호출 시 사용)")]
    [SerializeField] private bool autoFireOnEnable = false; // 기본 꺼둔다
    [SerializeField] private float defaultSpeed = 12f;
    [SerializeField] private float defaultLifeTime = 5f;
    [SerializeField] private float angleOffsetDeg = 0f; // 스프라이트 보정각

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        col.isTrigger = true;

        // 권장: Dynamic + 낮은 Linear Drag
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearDamping = 0f; // 필요시 0~0.1
    }

    private void OnEnable()
    {
        fired = false;
        CancelInvoke();

        if (autoFireOnEnable)
            StartCoroutine(Co_AutoFireNextFrame()); // Init이 먼저 올 기회를 준다
    }

    private IEnumerator Co_AutoFireNextFrame()
    {
        yield return null; // 한 프레임 대기
        if (fired) yield break;

        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            Fire((player.position - transform.position).normalized,
                 defaultSpeed, defaultLifeTime, damage, owner, ~0);
    }

    /// <summary>외부에서 반드시 호출(추천): 즉시 회전/속도/수명 설정</summary>
    public void Init(Vector2 direction, float dmg, GameObject owner, LayerMask hitMask, float speed, float lifeTime)
    {
        this.owner = owner;
        this.damage = dmg;
        this.hitMask = hitMask;
        Fire(direction.normalized, speed, lifeTime, dmg, owner, hitMask);
    }

    private void Fire(Vector2 dir, float speed, float lifeTime, float dmg, GameObject owner, LayerMask mask)
    {
        Debug.Log(gameObject.name + " speed : " + speed);
        // 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
        rb.SetRotation(angle);

        // 속도 즉시 적용 (Unity 6 권장 속성)
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = dir * speed;
#else
        rb.velocity = dir * speed;
#endif

        // 수명
        if (lifeTime > 0f) Invoke(nameof(Despawn), lifeTime);

        // 상태
        this.damage = dmg;
        this.owner = owner;
        this.hitMask = mask;
        fired = true;
    }

    private void Despawn()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.gameObject) return;
        if (owner && other.transform.IsChildOf(owner.transform)) return; // 자기 팀 무시
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var reactor = other.GetComponent<IHitReactor>();
        if (reactor != null)
        {
            reactor.OnAttacked(damage);
            Despawn();
        }
    }

    // 디버깅 팁: 누가 속도를 덮는지 확인하고 싶으면 열어둬
    // private Vector2 last;
    // private void FixedUpdate()
    // {
    // #if UNITY_6000_0_OR_NEWER
    //     var v = rb.linearVelocity;
    // #else
    //     var v = rb.velocity;
    // #endif
    //     if (v != last) { Debug.Log($"[Projectile] vel={v} (frame {Time.frameCount})"); last = v; }
    // }
}
