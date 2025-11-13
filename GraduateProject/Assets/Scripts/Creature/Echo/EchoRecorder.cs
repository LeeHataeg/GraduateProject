using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 보스전 중에만 활성화 시작 종료는 Manager급 코드가 관리
[DisallowMultipleComponent]
public class EchoRecorder : MonoBehaviour
{
    public float sampleDeltaTime = 0.05f;

    IAnimationController anim;
    EchoTape tape;
    float totalTime, sampleTime;

    Rigidbody2D rb;
    HealthController hc;

    bool lastMove;
    float lastHp;
    const float MOVE_EPS = 0.05f;   // 잡음 고려
    const float HP_EPS = 0.0001f;

    void Awake()
    {
        anim = GetComponent<IAnimationController>();
        rb = GetComponent<Rigidbody2D>();
        hc = GetComponent<HealthController>();
    }

    public void BeginRecord()
    {
        totalTime = 0f; sampleTime = 0f;
        tape = new EchoTape();
        lastMove = false;

        if (hc != null) 
            lastHp = hc.CurrentHp;

        // 오브젝트 활성화시켜 fixedUpdate 실시
        enabled = true;
    }

    public EchoTape EndRecord(bool wasClear)
    {
        enabled = false;
        if (tape != null)
        {
            tape.length = totalTime;
            tape.wasClear = wasClear;

            // 사망 관련 애니메이션 클립의 호출 변수를 마지막에 기록
            if (!wasClear)
            {
                float tt = Mathf.Max(0f, totalTime - 0.01f);
                tape.animParams.Add(new EchoTape.AnimParamEvt { t = tt, type = "bool", name = "isDeath", value = 1 });
                tape.animParams.Add(new EchoTape.AnimParamEvt { t = tt, type = "trig", name = "4_Death", value = 0 });
            }

            // 장비와 외형 기록
            TrySnapshotEquipment(tape);
            TrySnapshotVisuals(tape);
        }
        return tape;
    }

    void FixedUpdate()
    {
        if (tape == null) // Begin하지 않았거나 End되었을 때 강종
            return;

        totalTime += Time.fixedDeltaTime;
        sampleTime += Time.fixedDeltaTime;

        // 이동 중인지 여부 확인
        bool moving = false;
#if UNITY_6000_0_OR_NEWER
        // RB로부터 속도를 읽어서 이동 여부 판단
        // magnitude :  벡터 크기(연산 비쌈)
        // sqrMagnitudE : 벡터 크기 제곱 (연산 개꿀)
        if (rb != null)
            moving = rb.linearVelocity.sqrMagnitude > (MOVE_EPS * MOVE_EPS);
#else
        if (rb != null)
            moving = rb.velocity.sqrMagnitude > (MOVE_EPS * MOVE_EPS);
#endif
        if (sampleTime >= sampleDeltaTime)
        {
            sampleTime = 0f;

            // 프레임 신규 생성
            var f = new EchoTape.Frame
            {
                t = totalTime,
                pos = transform.position,
                faceRight = transform.localScale.x >= 0f,
                clip = anim?.GetCurClipname()
            };
            tape.frames.Add(f);

            // 이동 감지? 바로 Move 이벤트
            if (moving != lastMove)
            {
                tape.animParams.Add(new EchoTape.AnimParamEvt
                {
                    t = totalTime,
                    type = "bool",
                    name = "1_Move",
                    value = moving ? 1 : 0
                });
                lastMove = moving;
            }

            // 피격? 3_Damaged 트리거
            if (hc != null)
            {
                float cur = hc.CurrentHp;
                if (cur < lastHp - HP_EPS)
                {
                    tape.animParams.Add(new EchoTape.AnimParamEvt
                    {
                        t = totalTime,
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
        tape?.events.Add(new EchoTape.ActionEvt { t = totalTime, kind = "AtkBegin", id = id, value = factor });
        tape?.animParams.Add(new EchoTape.AnimParamEvt { t = totalTime, type = "trig", name = "2_Attack", value = 0 });
    }

    public void MarkActionEnd(string id)
        => tape?.events.Add(new EchoTape.ActionEvt { t = totalTime, kind = "AtkEnd", id = id, value = 0f });

    // 아이템 사용 기록
    public void MarkItemUsed(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            tape?.usedItemIds.Add(itemId);
    }

    //아이템 장착 상태 기록
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

    // Player오브젝트 하위의 모든 SpriterRenderer에 대하여 저장.
    private void TrySnapshotVisuals(EchoTape dest)
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
