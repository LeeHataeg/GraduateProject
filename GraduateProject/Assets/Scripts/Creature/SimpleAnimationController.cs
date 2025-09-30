using UnityEngine;


public class SimpleAnimationController : MonoBehaviour, IAnimationController
{
    private Animator animator;

    void Awake()
    {
        // 자식 오브젝트에 붙은 Animator를 찾아서 할당
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[{nameof(SimpleAnimationController)}] Animator를 찾을 수 없습니다. " +
                           $"자식에 Animator 컴포넌트가 붙어 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 해당 상태로 바로 전환합니다. (State 이름으로 Play)
    /// </summary>
    public void Play(string clipName)
    {
        if (animator == null) return;
        animator.Play(clipName);
    }

    /// <summary>
    /// 현재 재생 중인 모든 트리거/Bool 값을 초기화하고 Idle 상태로 되돌립니다.
    /// </summary>
    public void Stop()
    {
        if (animator == null) return;
        // 필요한 경우 “Idle” 상태로 전환하거나 트리거를 리셋할 수 있습니다.
        // Animator Controller에 Idle 이름의 상태가 있어야 합니다.
        animator.Play("Idle");

        // (선택) 모든 트리거 리셋
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(param.name);
            else if (param.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(param.name, false);
        }
    }

    /// <summary>
    /// Bool 파라미터를 설정합니다.
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        if (animator == null) return;

        if (!HasParameter(paramName, AnimatorControllerParameterType.Bool))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Bool 파라미터 '{paramName}'을(를) 찾을 수 없습니다.");
            return;
        }
        animator.SetBool(paramName, value);
    }

    /// <summary>
    /// Trigger 파라미터를 설정합니다.
    /// </summary>
    public void SetTrigger(string paramName)
    {
        if (animator == null) return;

        if (!HasParameter(paramName, AnimatorControllerParameterType.Trigger))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Trigger 파라미터 '{paramName}'을(를) 찾을 수 없습니다.");
            return;
        }
        animator.SetTrigger(paramName);
    }

    /// <summary>
    /// Animator에 해당 파라미터가 존재하는지 확인합니다.
    /// </summary>
    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == type)
                return true;
        }
        return false;
    }

    public void SetFloat(string name, float value)
    {
        throw new System.NotImplementedException();
    }

    public void SetInt(string name, int value)
    {
        throw new System.NotImplementedException();
    }

    public string GetCurClipname()
    {
        throw new System.NotImplementedException();
    }
}

