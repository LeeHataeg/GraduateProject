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
    [Range(0f, 1f)] public float toNextPhaseHpRate = 0.6f;

    [Header("Moves")]
    public List<Entry> moves = new();

    /// <summary>
    /// 1차: CanRun 충족 후보 중 가중치 랜덤
    /// 2차: (모두 실패 시) 조건 무시하고 아무거나 1개라도 반환 → '공격 안 함' 방지용 안전장치
    /// </summary>
    public BossMoveSO Pick(BossContext ctx)
    {
        // 1) 조건 만족 후보
        var cand = new List<Entry>();
        foreach (var e in moves)
            if (e.move != null && e.move.CanRun(ctx)) cand.Add(e);

        if (cand.Count > 0)
        {
            float total = Mathf.Max(0.0001f, cand.Sum(e => e.weight));
            float r = Random.value * total;
            foreach (var e in cand)
            {
                r -= e.weight;
                if (r <= 0) return e.move;
            }
            return cand[cand.Count - 1].move;
        }

        // 2) 전부 조건 미충족 → 아무거나 하나(첫 유효 move) 강제 선택
        foreach (var e in moves)
            if (e.move != null) return e.move;

        return null;
    }
}
