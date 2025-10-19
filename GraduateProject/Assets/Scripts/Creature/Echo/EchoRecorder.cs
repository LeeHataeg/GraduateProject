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
        enabled = false;
        if (tape != null) { tape.length = t; tape.wasClear = wasClear; }
        return tape;
    }

    void FixedUpdate()
    {
        if (tape == null) return;
        t += Time.fixedDeltaTime;
        acc += Time.fixedDeltaTime;
        if (acc >= sampleDt)
        {
            acc = 0f;
            var f = new EchoTape.Frame
            {
                t = t,
                pos = transform.position,
                faceRight = transform.localScale.x >= 0f,
                clip = anim?.GetCurClipname()
            };
            tape.frames.Add(f);
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
