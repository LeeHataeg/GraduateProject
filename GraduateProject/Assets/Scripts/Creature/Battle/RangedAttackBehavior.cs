using UnityEngine;

[DisallowMultipleComponent]
public class RangedAttackBehavior : MonoBehaviour, IAttackBehavior
{
    [SerializeField] private float range = 6.0f; // 사거리(발사 조건)
    [SerializeField] private Transform firePoint;

    [Header("Runtime Config (set by Assembler)")]
    [SerializeField] private Bullet projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLife = 3f;
    [SerializeField] private LayerMask hitLayers;

    public float Range => range;

    public void Configure(Bullet prefab, float speed, float life, LayerMask mask, float rangeOverride)
    {
        if (prefab) projectilePrefab = prefab;
        if (speed > 0f) projectileSpeed = speed;
        if (life > 0f) projectileLife = life;
        hitLayers = mask;
        if (rangeOverride > 0f) range = rangeOverride;
    }

    /// <summary>Assembler가 런타임에 FirePoint를 주입할 수 있도록</summary>
    public void SetFirePoint(Transform t) => firePoint = t;

    public void Execute(Vector2 position, float dmg, float atkRange)
    {
        if (!projectilePrefab) return;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!player) return;

        Vector2 origin = firePoint ? (Vector2)firePoint.position : position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;

        var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
        p.Init(dir, dmg, gameObject, hitLayers, projectileSpeed, projectileLife);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(firePoint.position, firePoint.position + Vector3.right * 0.5f);
        }
    }
#endif
}
