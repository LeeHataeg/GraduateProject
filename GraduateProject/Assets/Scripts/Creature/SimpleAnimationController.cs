using UnityEngine;

public class SimpleAnimationController : MonoBehaviour, IAnimationController
{
    Animator animator;
    void Awake() => animator = GetComponentInChildren<Animator>();
    public void Play(string clipName) => animator.Play(clipName);
    public void Stop() => animator.StopPlayback();
}
