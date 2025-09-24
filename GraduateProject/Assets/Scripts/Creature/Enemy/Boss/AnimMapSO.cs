using static Define;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Anim Map")]
public class AnimMapSO : ScriptableObject
{
    [System.Serializable]
    public struct Pair
    {
        public AnimKey key;
        [Tooltip("Animator 상태 이름 또는 Bool 파라미터 이름")]
        public string param;
        [Tooltip("Bool 파라미터면 true (루프/토글), 상태 재생이면 false")]
        public bool isBool;
        [Tooltip("isBool일 때 기본 세팅값")]
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

    public void Play(IAnimationController anim, AnimKey key, bool? boolOverride = null)
    {
        if (anim == null) return;
        if (_map == null || _map.Count == 0) Build();
        if (!_map.TryGetValue(key, out var p)) return;

        if (p.isBool) anim.SetBool(p.param, boolOverride ?? p.defaultBoolValue);
        else anim.SetTrigger(p.param);
    }
}