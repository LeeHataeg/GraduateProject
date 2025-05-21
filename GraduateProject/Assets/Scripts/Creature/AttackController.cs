using UnityEngine;
using static Define;

public class AttackController : MonoBehaviour
{
    private IAttackBehavior currentAttack;

    void Awake()
    {
        // 장비·스킬에 따라 공격 전략을 주입
        currentAttack = new MeleeAttacker();
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    var ctx = new AttackContext
        //    {
        //        Attacker = gameObject,
        //        Target = Fin(""),
        //        Direction = (FindTarget().position - transform.position).normalized
        //    };
        //    currentAttack.Execute(ctx);
        //}
    }
}
