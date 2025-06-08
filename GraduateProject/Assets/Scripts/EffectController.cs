using UnityEngine;

public class EffectController : MonoBehaviour
{
    private float dmg;
    private float duration;

    public void SetDmg(float dmg)
    {
        this.dmg = dmg;
    }

    public void SetDuration(float duration)
    {
        this.duration = duration;
    }

    public void OnEnable()
    {
        // TODO-PoolManager 반납으로 개선할 예정
        Destroy(this.gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        //
    }
}
