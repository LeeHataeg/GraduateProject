using System.Collections;
using UnityEngine;

/// <summary>
/// 샤먼 전용: 주기적으로 주변 아군에게 버프 스택을 부여.
/// </summary>
[DisallowMultipleComponent]
public class BuffAura : MonoBehaviour
{
    [Header("Aura")]
    public float radius = 4f;
    public float duration = 6f;
    public float refreshRate = 1.0f;
    public float dmgMultiplier = 1.25f; // 1.25 = +25%

    [Header("Filter")]
    public LayerMask allyMask; // 아군이 속한 레이어

    private Coroutine routine;

    private void OnEnable()
    {
        routine = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(refreshRate);
        while (true)
        {
            ApplyOnce();
            yield return wait;
        }
    }

    private void ApplyOnce()
    {
        var center = (Vector2)transform.position;
        var hits = Physics2D.OverlapCircleAll(center, radius, allyMask);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (!h.TryGetComponent<DamageMultiplier>(out var mult))
                mult = h.gameObject.AddComponent<DamageMultiplier>();

            // 동일 출처 오라 중복 시간 갱신
            mult.ApplyOrRefreshStack(source: this, duration, dmgMultiplier);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
