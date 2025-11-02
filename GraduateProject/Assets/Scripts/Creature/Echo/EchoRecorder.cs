using UnityEngine;

/// 보스전 중에만 enable. Begin/End는 EchoManager가 호출.
[DisallowMultipleComponent]
public class EchoRecorder : MonoBehaviour
{
    public float sampleDt = 0.05f; // 20Hz
    IAnimationController anim;
    EchoTape tape;
    float t, acc;

    void Awake() => anim = GetComponent<IAnimationController>();

    public void BeginRecord()
    {
        t = 0f; acc = 0f;
        tape = new EchoTape();
        enabled = true;
    }

    public EchoTape EndRecord(bool wasClear)
    {
        // 파괴/비활성 타이밍에서도 예외 없이 종료되도록 가드
        if (this != null)
        {
            try
            {
                if (isActiveAndEnabled)
                    enabled = false;
            }
            catch { /* 파괴 직후 프레임 안전망 */ }
        }

        if (tape != null) { tape.length = t; tape.wasClear = wasClear; }
        return tape;
    }

    void FixedUpdate()
    {
        if (tape == null) return;

        acc += Time.fixedDeltaTime;
        while (acc >= sampleDt)
        {
            acc -= sampleDt;
            t += sampleDt;

            var tr = transform;
            var pos = (Vector2)tr.position;

            // Unity 6: velocity → linearVelocity
            Vector2 vel = Vector2.zero;
            var rb = GetComponent<Rigidbody2D>();
            if (rb) vel = rb.linearVelocity;

            // EchoTape.Frame에는 속도가 없고, facing만 저장한다.
            bool faceRight;
            if (Mathf.Abs(vel.x) > 1e-4f)
            {
                faceRight = vel.x > 0f;
            }
            else
            {
                // 정지 시에는 스케일 기준(또는 필요하면 스프라이트 flipX 등으로 대체)
                faceRight = tr.localScale.x >= 0f;
            }

            string clip = null;
            if (anim != null)
            {
                try { clip = anim.GetCurClipname(); }
                catch { /* 애니 컨트롤러 전환 타이밍 가드 */ }
            }

            tape.frames.Add(new EchoTape.Frame
            {
                t = t,
                pos = pos,
                faceRight = faceRight,
                clip = clip
            });
        }
    }

    // ── 공격/스킬 윈도우 기록(애니/스킬 이벤트에서 호출) ──
    public void MarkActionBegin(string id, float factor = 1f)
        => tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkBegin", id = id, value = factor });
    public void MarkActionEnd(string id)
        => tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkEnd", id = id, value = 0f });

    // ── 아이템 사용 기록(인벤토리 브리지에서 호출) ──
    public void MarkItemUsed(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            tape?.usedItemIds.Add(itemId);
    }
}
