using UnityEngine;

/// IAnimationController 어댑터: 상태 이름으로 CrossFade, Bool 토글 지원
public class AnimatorAdapter : MonoBehaviour, IAnimationController
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("상태 전환 시 CrossFade 시간(초)")]
    private float crossFadeTime = 0.05f;

    void Reset() { if (!animator) animator = GetComponent<Animator>(); }

    public void SetTrigger(string paramOrStateName)
    {
        if (!animator) return;
        animator.CrossFadeInFixedTime(paramOrStateName, crossFadeTime, 0, 0f);
    }

    public void SetBool(string name, bool value)
    {
        if (!animator) return;
        animator.SetBool(name, value);
    }

    public void SetFloat(string name, float value)
    {
        if (!animator) return;
        animator.SetFloat(name, value);
    }

    public void SetInt(string name, int value)
    {
        if (!animator) return;
        animator.SetInteger(name, value);
    }

    public void Play(string clipName)
    {
        throw new System.NotImplementedException();
    }

    public void Stop()
    {
        throw new System.NotImplementedException();
    }
}
