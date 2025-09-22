using UnityEngine;

public class BossContext
{
    public Transform Self;
    public Transform AttackOrigin;
    public Transform Player;
    public IAnimationController Anim;
    public AnimMapSO Anims;
    public HealthController Health;
    public Rigidbody2D RB;

    public System.Action<bool> LockMove = _ => { };
}