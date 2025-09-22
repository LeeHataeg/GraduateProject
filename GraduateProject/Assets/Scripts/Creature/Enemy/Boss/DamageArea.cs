using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageArea : MonoBehaviour
{
    public float dps = 6f;
    public LayerMask hitMask;
    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;
        var hr = other.GetComponent<IHitReactor>();
        if (hr != null)
        {
            hr.OnAttacked(dps * Time.deltaTime);
        }
    }
}
