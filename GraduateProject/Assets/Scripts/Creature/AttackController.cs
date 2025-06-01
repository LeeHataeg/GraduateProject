using UnityEngine;
using static Define;

public class AttackController : MonoBehaviour/*, IAttackBehavior*/
{
    private IAttackBehavior currentAttack;

    public float Range => throw new System.NotImplementedException();

    public float Damage => throw new System.NotImplementedException();

    public void Execute(AttackContext context)
    {
        throw new System.NotImplementedException();
    }

    void Awake()
    {
        currentAttack = new MeleeAttacker();
    }

    void Update()
    {
        
    }
}
