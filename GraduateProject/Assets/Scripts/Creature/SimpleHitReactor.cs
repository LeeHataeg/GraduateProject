using UnityEngine;

public class SimpleHitReactor : MonoBehaviour, IHitReactor
{
    public void OnAttack(float damage, Vector2 hitDirection)
    {
        // 무적 프레임, 넉백 등
        var rb = GetComponent<Rigidbody2D>();
        rb?.AddForce(hitDirection.normalized * 5f, ForceMode2D.Impulse);
    }
}
