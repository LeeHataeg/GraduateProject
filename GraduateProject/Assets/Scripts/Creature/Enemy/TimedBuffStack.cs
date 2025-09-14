using System.Collections;
using UnityEngine;

/// <summary>
/// 특정 source에서 온 버프 1개를 표현. 시간이 끝나면 자동 제거.
/// </summary>
[DisallowMultipleComponent]
public class TimedBuffStack : MonoBehaviour
{
    private DamageMultiplier owner;
    private Object source;
    private float duration;
    public float Multiplier { get; private set; } = 1f;

    private Coroutine co;

    public void Initialize(DamageMultiplier owner, Object source, float duration, float mul)
    {
        this.owner = owner;
        this.source = source;
        this.duration = duration;
        this.Multiplier = mul;

        co = StartCoroutine(Timer());
    }

    public void Refresh(float newDuration, float newMul)
    {
        duration = Mathf.Max(duration, newDuration);
        Multiplier = Mathf.Max(Multiplier, newMul); // 같은 소스에선 더 강한 값 유지(정책)
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Timer());
        owner?.Recalc();
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(duration);
        owner?.Remove(source);
        Destroy(this);
    }

    private void OnDestroy()
    {
        // owner.Remove는 Timer에서 호출됨. 중복 호출 방지.
    }
}
