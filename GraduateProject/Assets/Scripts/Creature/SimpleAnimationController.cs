using UnityEngine;

public class SimpleAnimationController : MonoBehaviour, IAnimationController
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[{nameof(SimpleAnimationController)}] Animator를 찾을 수 없습니다. " +
                           $"자식에 Animator 컴포넌트가 붙어 있는지 확인하세요.");
        }
    }

    /// <summary>해당 상태로 즉시 전환.</summary>
    public void Play(string clipName)
    {
        if (animator == null) return;
        animator.Play(clipName);
    }

    /// <summary>트리거/Bool 리셋하고 Idle로.</summary>
    public void Stop()
    {
        if (animator == null) return;

        // Idle로 복귀 (프로젝트의 Idle 스테이트명이 다르면 맞춰 변경)
        animator.Play("Idle");

        // 모든 Trigger/Bool 리셋
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(param.name);
            else if (param.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(param.name, false);
        }
    }

    /// <summary>Bool 파라미터 설정.</summary>
    public void SetBool(string paramName, bool value)
    {
        if (animator == null) return;
        if (!HasParameter(paramName, AnimatorControllerParameterType.Bool))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Bool 파라미터 '{paramName}' 없음.");
            return;
        }
        animator.SetBool(paramName, value);
    }

    /// <summary>Trigger 파라미터 설정.</summary>
    public void SetTrigger(string paramName)
    {
        if (animator == null) return;
        if (!HasParameter(paramName, AnimatorControllerParameterType.Trigger))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Trigger 파라미터 '{paramName}' 없음.");
            return;
        }
        animator.SetTrigger(paramName);
    }

    /// <summary>Float 파라미터 설정.</summary>
    public void SetFloat(string name, float value)
    {
        if (animator == null) return;
        if (!HasParameter(name, AnimatorControllerParameterType.Float))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Float 파라미터 '{name}' 없음.");
            return;
        }
        animator.SetFloat(name, value);
    }

    /// <summary>Int 파라미터 설정.</summary>
    public void SetInt(string name, int value)
    {
        if (animator == null) return;
        if (!HasParameter(name, AnimatorControllerParameterType.Int))
        {
            Debug.LogWarning($"[{nameof(SimpleAnimationController)}] Int 파라미터 '{name}' 없음.");
            return;
        }
        animator.SetInteger(name, value);
    }

    /// <summary>
    /// 현재 재생 중인 클립 이름을 반환. 클립을 찾을 수 없으면 빈 문자열 반환.
    /// 여러 레이어일 경우 가중치>0 레이어 우선, 없으면 0번 레이어 기준.
    /// </summary>
    public string GetCurClipname()
    {
        if (animator == null) return string.Empty;

        // 우선순위: 가중치 > 0 인 레이어를 먼저 스캔
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (i == 0 || animator.GetLayerWeight(i) > 0.01f)
            {
                var name = GetCurrentClipNameForLayer(i);
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        // 전혀 못 찾으면 빈 문자열
        return string.Empty;
    }

    private string GetCurrentClipNameForLayer(int layer)
    {
        // 현재 레이어에서 재생 중인 클립 정보
        var clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
            return clips[0].clip.name;

        // 클립이 비어 있으면 상태 이름(해시 기반)이라서 정확한 문자열을 알 수 없음 → 빈 문자열 반환
        // (원한다면 "State_<hash>" 형태로 반환하도록 바꿀 수 있음)
        return string.Empty;
    }

    /// <summary>Animator에 파라미터 존재 여부.</summary>
    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == type)
                return true;
        }
        return false;
    }
}
