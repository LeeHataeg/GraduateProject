using UnityEngine;

[DisallowMultipleComponent]
public class EchoPlayback : MonoBehaviour
{
    [Header("Hitbox & Visual")]
    public AttackHitbox hitbox;           // 선택(있으면 Ghost도 공격 가능)
    public SpriteRenderer sr;             // 투명도
    public float alpha = 0.4f;            // 외형 투명도
    [Tooltip("유령은 본체 대비 몇 %의 데미지를 낼지")]
    public float damageScale = 0.5f;      // “스탯 50% 이하” → 데미지만 50%로 보정

    [Header("Fallback Attack Heuristic")]
    [Tooltip("테이프에 Action 이벤트가 없을 때, clip 이름에 이 문자열이 포함되면 '공격 중'으로 간주")]
    public string attackClipKeyword = "Attack";
    [Tooltip("이벤트가 없고 clip 키워드가 감지되면 자동으로 공격 윈도우를 열고, 키워드가 사라지면 닫습니다.")]
    public bool useHeuristicIfNoEvents = true;

    EchoTape tape;
    int fi, ei;
    float t;

    // 내부 상태(휴리스틱용)
    bool windowOpenByHeuristic = false;

    public void Load(EchoTape t_)
    {
        tape = t_;
        fi = 0;
        ei = 0;
        t = 0f;

        // 프리팹에서 깜빡한 경우 자동 보강
        if (!hitbox) hitbox = GetComponentInChildren<AttackHitbox>(true);

        // 데미지 스케일 보정
        if (hitbox) hitbox.baseDamage *= damageScale;
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[EchoPlayback] 'hitbox'가 비어 있습니다. 유령의 공격 판정이 생성되지 않습니다.");
#endif
        }
    }

    public void SetAlpha(float a)
    {
        alpha = a;
        if (sr)
        {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    void Update()
    {
        if (tape == null || tape.frames.Count == 0)
        {
            Destroy(gameObject);
            return;
        }
        t += Time.deltaTime;

        // 위치 보간
        while (fi + 1 < tape.frames.Count && tape.frames[fi + 1].t <= t) fi++;
        var a = tape.frames[Mathf.Clamp(fi, 0, tape.frames.Count - 1)];
        var b = tape.frames[Mathf.Clamp(fi + 1, 0, tape.frames.Count - 1)];
        float u = Mathf.Approximately(b.t, a.t) ? 0f : (t - a.t) / (b.t - a.t);

        transform.position = Vector2.Lerp(a.pos, b.pos, u);

        var s = transform.localScale;
        s.x = (a.faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x));
        transform.localScale = s;

        // ─────────────────────────────────────────────────────────────
        // 1) 기록된 이벤트 재생(정석 경로)
        // ─────────────────────────────────────────────────────────────
        while (ei < tape.events.Count && tape.events[ei].t <= t)
        {
            var e = tape.events[ei++];
            if (e.kind == "AtkBegin") hitbox?.BeginWindow();
            else if (e.kind == "AtkEnd") hitbox?.EndWindow();
        }

        // ─────────────────────────────────────────────────────────────
        // 2) 대체 경로(이벤트가 하나도 없을 때만): 클립 이름 휴리스틱
        //    - clip에 attackClipKeyword가 포함되면 공격 중으로 간주 → 윈도우 open
        //    - 키워드가 사라지면 close
        // ─────────────────────────────────────────────────────────────
        if (useHeuristicIfNoEvents && tape.events.Count == 0 && hitbox != null)
        {
            string curClip = a.clip ?? string.Empty;
            bool looksLikeAttack = !string.IsNullOrEmpty(curClip) && curClip.IndexOf(attackClipKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (looksLikeAttack && !windowOpenByHeuristic)
            {
                hitbox.BeginWindow();
                windowOpenByHeuristic = true;
            }
            else if (!looksLikeAttack && windowOpenByHeuristic)
            {
                hitbox.EndWindow();
                windowOpenByHeuristic = false;
            }
        }
    }

    private void OnDestroy()
    {
        // 안전장치: 열려 있던 휴리스틱 윈도우 닫기
        if (windowOpenByHeuristic && hitbox != null)
        {
            try { hitbox.EndWindow(); } catch { /* no-op */ }
            windowOpenByHeuristic = false;
        }
    }
}
