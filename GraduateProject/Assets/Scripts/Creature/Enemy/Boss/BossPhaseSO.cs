using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Boss/Phase")]
public class BossPhaseSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public BossMoveSO move;
        [Range(0f, 1f)] public float weight = 0.25f;
    }

    [Header("Phase Flow")]
    public AnimKey onEnterPlay = AnimKey.Idle;
    [Tooltip("다음 페이즈로 넘어갈 HP 비율(Phase1에서만 사용)")]
    public float toNextPhaseHpRate = 0.5f; // 반피

    [Header("Moves")]
    public List<Entry> moves = new();

    public BossMoveSO Pick(BossContext ctx)
    {
        var cand = new List<Entry>();
        foreach (var e in moves)
            if (e.move != null && e.move.CanRun(ctx)) cand.Add(e);

        if (cand.Count == 0) return null;

        float total = Mathf.Max(0.0001f, cand.Sum(e => e.weight));
        float r = Random.value * total;
        foreach (var e in cand)
        {
            r -= e.weight;
            if (r <= 0) return e.move;
        }
        return cand[cand.Count - 1].move;
    }
}