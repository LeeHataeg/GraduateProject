using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 버프 스택을 모아 최종 공격 배수를 계산.
/// EnemyController가 공격 직전 dmg에 곱한다.
/// </summary>
[DisallowMultipleComponent]
public class DamageMultiplier : MonoBehaviour
{
    private readonly Dictionary<Object, TimedBuffStack> stacks = new();
    private float totalMul = 1f;

    public float Apply(float dmg) => dmg * totalMul;

    public void ApplyOrRefreshStack(Object source, float duration, float mul)
    {
        //if (mul <= 1f) return; // 1.0 이하는 이득 없음(필요시 허용 가능)
        //if (stacks.TryGetValue(source, out var s))
        //{
        //    s.Refresh(duration, mul);
        //}
        //else
        //{
        //    var stack = gameObject.AddComponent<TimedBuffStack>();
        //    stack.Initialize(this, source, duration, mul);
        //    stacks[source] = stack;
        //    Recalc();
        //}
    }

    internal void Remove(Object source)
    {
        //if (stacks.Remove(source))
        //    Recalc();
    }

    internal void Recalc()
    {
        //float mul = 1f;
        //foreach (var s in stacks.Values)
        //    mul *= s.Multiplier; // 곱연산 누적
        //totalMul = Mathf.Max(0.1f, mul); // 안전 하한
    }
}
