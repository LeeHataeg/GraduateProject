using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyControllerTemp : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float atkRange = 4f;
    private Transform player;
    private Animator animator;
    private Rigidbody2D rb;
    private float Curhp = 5f;
    private float atk = 2f;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Curhp <= 0f)
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;
        if (Curhp <= 0f) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float deltaX = player.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) > atkRange)
        {
            animator.SetBool("1_Move", true);
            Vector2 dir = new Vector2(Mathf.Sign(deltaX), 0);
            rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            animator.SetBool("1_Move", false);
            animator.SetTrigger("2_Attack");
            rb.linearVelocity = Vector2.zero;
        }


        if (deltaX > 0f)
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        else if (deltaX < 0f)
            transform.eulerAngles = Vector3.zero;
    }

    public void Damage(float damage)
    {
        Curhp -= damage;
    }

    public void Die()
    {
        animator.SetTrigger("4_Death");
        animator.SetBool("isDeath", true);
        Destroy(this.transform.parent.gameObject, 1.5f);
    }
}


