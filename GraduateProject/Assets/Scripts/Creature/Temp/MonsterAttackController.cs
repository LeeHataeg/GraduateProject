using UnityEngine;

[RequireComponent(typeof(MeleeAttackBehavior))]
public class MonsterAttackController : MonoBehaviour
{
    MeleeAttackBehavior attackBehavior;
    Transform player;

    [Header("AI Settings")]
    public float attackInterval = 2f;
    float cooldown;

    void Awake()
    {
        attackBehavior = GetComponent<MeleeAttackBehavior>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        // 플레이어가 일정 거리 안에 들어오면
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackBehavior.Range)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f)
            {
                DoAttack();
                cooldown = attackInterval;
            }
        }
    }

    void DoAttack()
    {
        var ctx = new AttackContext
        {
            Origin = transform.position,
            Direction = (player.position - transform.position).normalized,
            Attacker = GetComponent<BaseStat>(),
            Behavior = attackBehavior
        };
        attackBehavior.Execute(ctx);
    }
}