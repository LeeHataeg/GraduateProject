using System.Collections.Generic;
using UnityEngine;

/// IAnimationController 구현체: Animator 래퍼
/// - State: CrossFadeInFixedTime로 상태 전환
/// - Bool/Trigger: Animator 파라미터 직접 제어
/// - 현재 재생 중 클립/상태 조회 지원
[DisallowMultipleComponent]
public class AnimatorAdaptor : MonoBehaviour, IAnimationController
{
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeTime = 0.05f;

    private HashSet<int> _paramHashes;

    public Animator UnityAnimator => animator; // 상태 해시 판정에 사용

    void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    void Awake()
    {
        BuildParamCache();
    }

    private void BuildParamCache()
    {
        _paramHashes = new HashSet<int>();
        if (!animator) return;
        foreach (var p in animator.parameters)
            _paramHashes.Add(p.nameHash);
    }

    private bool HasParam(string name)
    {
        if (!animator) return false;
        if (_paramHashes == null) BuildParamCache();
        return _paramHashes.Contains(Animator.StringToHash(name));
    }

    public void SetTrigger(string paramOrStateName)
    {
        if (!animator) return;

        // 1) 파라미터가 있으면 Trigger
        if (HasParam(paramOrStateName))
        {
            animator.SetTrigger(paramOrStateName);
            return;
        }

        // 2) 없으면 상태로 간주하고 CrossFade
        animator.CrossFadeInFixedTime(paramOrStateName, crossFadeTime, 0, 0f);
    }

    public void SetBool(string name, bool value)
    {
        if (!animator) return;
        if (HasParam(name))
        {
            animator.SetBool(name, value);
        }
    }

    public void SetFloat(string name, float value)
    {
        if (!animator) return;
        if (HasParam(name)) animator.SetFloat(name, value);
    }

    public void SetInt(string name, int value)
    {
        if (!animator) return;
        if (HasParam(name)) animator.SetInteger(name, value);
    }

    // 상태 즉시 진입(경합 우회 응급처치용)
    public void ForcePlayImmediate(string stateOrPath, int layer = 0)
    {
        if (!animator) return;
        animator.Play(stateOrPath, layer, 0f);
        animator.Update(0f); // 즉시 반영
    }

    public void Play(string clipOrState)
    {
        // 필요 시 사용할 수 있도록 즉시 재생으로 구현
        ForcePlayImmediate(clipOrState, 0);
    }

    public void Stop()
    {
        // 필요 시 구현 (예: speed=0)
    }

    public string GetCurClipname()
    {
        if (!animator) return null;
        var infos = animator.GetCurrentAnimatorClipInfo(0);
        return (infos.Length > 0) ? infos[0].clip.name : null;
    }

    public int GetCurrentStateShortHash(int layer = 0)
    {
        if (!animator) return 0;
        return animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
    }
}
