using UnityEngine;

[DisallowMultipleComponent]
public class EchoPlayback : MonoBehaviour
{
    public AttackHitbox hitbox; // 선택(있으면 Ghost도 공격 가능)
    public SpriteRenderer sr;   // 투명도
    public float alpha = 0.4f;  // 외형 투명도
    public float damageScale = 0.5f; // “스탯 50% 이하” → 데미지만 50%로 보정

    EchoTape tape;
    int fi, ei; float t;

    public void Load(EchoTape t_)
    {
        tape = t_; fi = 0; ei = 0; t = 0f;
        if (hitbox) hitbox.baseDamage *= damageScale; // 기존 AttackHitbox 그대로 활용
    }

    public void SetAlpha(float a)
    {
        alpha = a;
        if (sr) { var c = sr.color; c.a = alpha; sr.color = c; }
    }

    void Update()
    {
        if (tape == null || tape.frames.Count == 0) { Destroy(gameObject); return; }
        t += Time.deltaTime;

        // 위치 보간
        while (fi + 1 < tape.frames.Count && tape.frames[fi + 1].t <= t) fi++;
        var a = tape.frames[Mathf.Clamp(fi, 0, tape.frames.Count - 1)];
        var b = tape.frames[Mathf.Clamp(fi + 1, 0, tape.frames.Count - 1)];
        float u = Mathf.Approximately(b.t, a.t) ? 0f : (t - a.t) / (b.t - a.t);
        transform.position = Vector2.Lerp(a.pos, b.pos, u);
        var s = transform.localScale; s.x = (a.faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x)); transform.localScale = s;

        // 이벤트 재생(히트윈도우)
        while (ei < tape.events.Count && tape.events[ei].t <= t)
        {
            var e = tape.events[ei++];
            if (e.kind == "AtkBegin") hitbox?.BeginWindow();
            else if (e.kind == "AtkEnd") hitbox?.EndWindow();
        }
    }
}
