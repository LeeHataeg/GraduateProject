using static Define;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Anim Map")]
public class AnimMapSO : ScriptableObject
{
    public enum ParamKind { State, Trigger, Bool }

    [System.Serializable]
    public struct Pair
    {
        public AnimKey key;

        [Tooltip("Animator 상태명 / 트리거명 / Bool 파라미터명 (kind에 따라 달라짐)")]
        public string param;

        [Tooltip("State, Trigger, Bool 중 무엇인지")]
        public ParamKind kind;

        [Tooltip("kind==Bool일 때 기본 세팅값")]
        public bool defaultBoolValue;
    }

    [SerializeField] private List<Pair> pairs = new();
    private Dictionary<AnimKey, Pair> _map;

    void OnEnable() => Build();

    public void Build()
    {
        _map = new Dictionary<AnimKey, Pair>(pairs.Count);
        foreach (var p in pairs) _map[p.key] = p;
    }

    /// <summary>
    /// boolOverride는 Bool 파라미터일 때만 의미 있음.
    /// </summary>
    public void Play(IAnimationController anim, AnimKey key, bool? boolOverride = null)
    {
        if (anim == null) return;
        if (_map == null || _map.Count == 0) Build();
        if (!_map.TryGetValue(key, out var p)) return;

        switch (p.kind)
        {
            case ParamKind.Bool:
                anim.SetBool(p.param, boolOverride ?? p.defaultBoolValue);
                break;

            case ParamKind.Trigger:
                anim.SetTrigger(p.param);
                break;

            case ParamKind.State:
                // IAnimationController가 상태 이름을 받아 CrossFade/Play 하도록 구현돼 있어야 함
                anim.Play(p.param);
                break;
        }
    }
}
