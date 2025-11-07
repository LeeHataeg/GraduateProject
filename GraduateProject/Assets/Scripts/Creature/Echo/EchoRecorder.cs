using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 보스전 중에만 활성화 시작 종료는 Manager급 코드가 관리
[DisallowMultipleComponent]
public class EchoRecorder : MonoBehaviour
{
    public float sampleDt = 0.05f;

    IAnimationController anim;
    EchoTape tape;
    float t, acc;

    Rigidbody2D rb;
    HealthController hc;

    bool lastMove;
    float lastHp;
    const float MOVE_EPS = 0.05f;
    const float HP_EPS = 1e-4f;

    void Awake()
    {
        anim = GetComponent<IAnimationController>();
        rb = GetComponent<Rigidbody2D>();
        hc = GetComponent<HealthController>();
    }

    public void BeginRecord()
    {
        t = 0f; acc = 0f;
        tape = new EchoTape();
        lastMove = false;

        if (hc != null) lastHp = hc.CurrentHp;

        enabled = true;
    }

    public EchoTape EndRecord(bool wasClear)
    {
        enabled = false;
        if (tape != null)
        {
            tape.length = t;
            tape.wasClear = wasClear;

            // 사망 관련 애니메이션 클립의 호출 변수를 마지막에 기록
            if (!wasClear)
            {
                float tt = Mathf.Max(0f, t - 0.01f);
                tape.animParams.Add(new EchoTape.AnimParamEvt { t = tt, type = "bool", name = "isDeath", value = 1 });
                tape.animParams.Add(new EchoTape.AnimParamEvt { t = tt, type = "trig", name = "4_Death", value = 0 });
            }

            // 장비와 외형 기록
            TrySnapshotEquipment(tape);
            TrySnapshotVisualsByPath(tape);
        }
        return tape;
    }

    void FixedUpdate()
    {
        if (tape == null) return;
        t += Time.fixedDeltaTime;
        acc += Time.fixedDeltaTime;

        // 이동 Bool 추적(변화 시점만 이벤트로 남김)
        bool moving = false;
#if UNITY_6000_0_OR_NEWER
        if (rb) moving = rb.linearVelocity.sqrMagnitude > (MOVE_EPS * MOVE_EPS);
#else
        if (rb) moving = rb.velocity.sqrMagnitude > (MOVE_EPS * MOVE_EPS);
#endif
        if (acc >= sampleDt)
        {
            acc = 0f;

            // 프레임(위치/좌우/클립)도 보조로 계속 저장
            var f = new EchoTape.Frame
            {
                t = t,
                pos = transform.position,
                faceRight = transform.localScale.x >= 0f,
                clip = anim?.GetCurClipname()
            };
            tape.frames.Add(f);

            // 이동 상태 변화 감지 시 1_Move Bool 이벤트 기록
            if (moving != lastMove)
            {
                tape.animParams.Add(new EchoTape.AnimParamEvt
                {
                    t = t,
                    type = "bool",
                    name = "1_Move",
                    value = moving ? 1 : 0
                });
                lastMove = moving;
            }

            // 피격(HP 감소) → 3_Damaged 트리거
            if (hc != null)
            {
                float cur = hc.CurrentHp;
                if (cur < lastHp - HP_EPS)
                {
                    tape.animParams.Add(new EchoTape.AnimParamEvt
                    {
                        t = t,
                        type = "trig",
                        name = "3_Damaged",
                        value = 0
                    });
                }
                lastHp = cur;
            }
        }
    }

    // 공격 이벤트 기록
    public void MarkActionBegin(string id, float factor = 1f)
    {
        tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkBegin", id = id, value = factor });
        tape?.animParams.Add(new EchoTape.AnimParamEvt { t = t, type = "trig", name = "2_Attack", value = 0 });
    }

    public void MarkActionEnd(string id)
        => tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkEnd", id = id, value = 0f });

    public void MarkItemUsed(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            tape?.usedItemIds.Add(itemId);
    }

    private void TrySnapshotEquipment(EchoTape dest)
    {
        var eq = GetComponentInChildren<EquipmentManager>(true);
        if (eq == null) return;

        foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
        {
            var item = eq.GetEquipped(slot);
            if (item != null)
            {
                dest.equipped.Add(new EchoTape.EquipEntry
                {
                    slot = slot.ToString(),
                    itemId = item.name
                });
            }
        }
    }

    private void TrySnapshotVisualsByPath(EchoTape dest)
    {
        var unitRoot = this.transform;
        var root = unitRoot.Find("Root");
        if (root == null)
        {
            return;
        }

        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            var relPath = GetRelativePath(sr.transform, root);
            if (string.IsNullOrEmpty(relPath)) continue;

            var vp = new EchoTape.VisualPart
            {
                path = relPath,
                sprite = sr.sprite ? sr.sprite.name : string.Empty,
                localPosOffset = Vector2.zero,
                localScaleMul = new Vector2(1f, 1f),
                sortingOffset = 0,
                enabled = sr.enabled,
                changeMaskInteraction = true,
                maskInteraction = (int)sr.maskInteraction
            };

            var mask = sr.GetComponent<SpriteMask>();
            vp.enablePartSpriteMask = (mask ? mask.enabled : false);

            dest.visualParts.Add(vp);
        }
    }

    private static string GetRelativePath(Transform t, Transform root)
    {
        if (t == null || root == null) return null;
        List<string> seg = new List<string>();
        var cur = t;
        while (cur != null && cur != root)
        {
            seg.Add(cur.name);
            cur = cur.parent;
        }
        if (cur != root) return null;
        seg.Reverse();
        return string.Join("/", seg);
    }
}
