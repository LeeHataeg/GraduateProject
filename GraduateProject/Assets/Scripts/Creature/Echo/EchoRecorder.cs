using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// 보스전 중에만 enable. Begin/End는 EchoManager가 호출.
[DisallowMultipleComponent]
public class EchoRecorder : MonoBehaviour
{
    public float sampleDt = 0.05f;
    IAnimationController anim;
    EchoTape tape;
    float t, acc;

    void Awake() 
    { 
        anim = GetComponent<IAnimationController>(); 
    }

    public void BeginRecord()
    {
        t = 0f; acc = 0f;
        tape = new EchoTape();
        enabled = true;
    }

    public EchoTape EndRecord(bool wasClear)
    {
        enabled = false;
        if (tape != null)
        {
            tape.length = t;
            tape.wasClear = wasClear;

            // 사망 당시 착용한 장비와 당시 플레이어의 외형 기록
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

    public void MarkActionBegin(string id, float factor = 1f)
        => tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkBegin", id = id, value = factor });
    public void MarkActionEnd(string id)
        => tape?.events.Add(new EchoTape.ActionEvt { t = t, kind = "AtkEnd", id = id, value = 0f });


    public void MarkItemUsed(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            tape?.usedItemIds.Add(itemId);
    }

    // ─────────────────────────────────────────────────────────
    //                        스냅샷
    // ─────────────────────────────────────────────────────────
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
                    itemId = item.name   // ScriptableObject.name 사용
                });
            }
        }
    }

    private void TrySnapshotVisualsByPath(EchoTape dest)
    {
        // 플레이어 구조: UnitRoot(this) / "Root"
        var unitRoot = this.transform;
        var root = unitRoot.Find("Root");
        if (root == null)
        {
            Debug.LogWarning("[EchoRecorder] 'Root'를 찾지 못해 외형 스냅샷을 생략합니다.");
            return;
        }

        // Root 하위 모든 SpriteRenderer 스냅샷
        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            var relPath = GetRelativePath(sr.transform, root);
            if (string.IsNullOrEmpty(relPath)) continue;

            // 기본값은 현재 값 자체를 기준으로 저장(고스트 적용 시 동일 상태 재현)
            var vp = new EchoTape.VisualPart
            {
                path = relPath,
                sprite = sr.sprite ? sr.sprite.name : string.Empty,
                localPosOffset = Vector2.zero,            // 현재 위치를 그대로 쓰므로 0
                localScaleMul = new Vector2(1f, 1f),      // 현재 스케일 그대로
                sortingOffset = 0,                        // 현재 sorting 그대로
                enabled = sr.enabled,
                changeMaskInteraction = true,
                maskInteraction = (int)sr.maskInteraction
            };

            // 선택 마스크 존재 여부
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
