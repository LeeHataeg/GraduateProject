using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Define;

[RequireComponent(typeof(EquipmentManager))]
[DisallowMultipleComponent]
public class EquipmentVisualController : MonoBehaviour
{
    // 기본 스케일이 (0,0,1) 같이 비정상일 때 곱셈 결과가 0이 되는 것을 방지하기 위한 보정
    private static Vector3 BaseScale(Vector3 s)
    {
        float bx = Mathf.Approximately(s.x, 0f) ? 1f : s.x;
        float by = Mathf.Approximately(s.y, 0f) ? 1f : s.y;
        float bz = Mathf.Approximately(s.z, 0f) ? 1f : s.z;
        return new Vector3(bx, by, bz);
    }

    [Serializable]
    public class PartBinding
    {
        public Define.BodyPart part;
        public SpriteRenderer renderer;       // 필수
        public SpriteMask optionalMask;       // 선택(모자 등)

        // 기본 상태 백업
        [HideInInspector] public Sprite defaultSprite;
        [HideInInspector] public Vector3 defaultLocalPos;
        [HideInInspector] public Vector3 defaultLocalScale;
        [HideInInspector] public int defaultSortingOrder;
        [HideInInspector] public bool defaultEnabled;
        [HideInInspector] public SpriteMaskInteraction defaultMaskInteraction;
        [HideInInspector] public bool defaultMaskEnabled;
    }

    [Serializable]
    public class SlotDefaultTargets
    {
        public EquipmentSlot slot;
        public Define.BodyPart[] defaultParts;  // visuals 없을 때 icon을 꽂기 타겟들
    }

    [Header("전체 파트 바인딩")]
    public PartBinding[] parts;

    [Header("슬롯별 기본 아이콘 꽂기 대상")]
    public SlotDefaultTargets[] slotDefaults =
    {
        new SlotDefaultTargets{ slot=EquipmentSlot.Head,  defaultParts=new[]{ Define.BodyPart.Hat } },
        new SlotDefaultTargets{ slot=EquipmentSlot.Chest, defaultParts=new[]{ Define.BodyPart.Chest } },
        new SlotDefaultTargets{ slot=EquipmentSlot.Legs,  defaultParts=new[]{ Define.BodyPart.LegL, Define.BodyPart.LegR } },
        new SlotDefaultTargets{ slot=EquipmentSlot.Weapon,defaultParts=new[]{ Define.BodyPart.WeaponR } },
        // 필요에 맞게 추가/수정
    };

    private EquipmentManager eq;

    // 슬롯별로 방금 적용해 변경한 파트 목록 추적(복원용)
    private readonly Dictionary<EquipmentSlot, List<Define.BodyPart>> modifiedBySlot = new();

    // 빠른 조회를 위한 맵
    private Dictionary<Define.BodyPart, PartBinding> map;
    private Dictionary<EquipmentSlot, Define.BodyPart[]> defaultMap;

    void Awake()
    {
        eq = GetComponent<EquipmentManager>() ?? FindFirstObjectByType<EquipmentManager>();
        eq.OnEquippedChanged -= OnEquippedChanged;
        eq.OnEquippedChanged += OnEquippedChanged;

        map = parts.Where(p => p != null && p.renderer != null)
                   .ToDictionary(p => p.part, p => p);

        defaultMap = slotDefaults.ToDictionary(d => d.slot, d => d.defaultParts ?? Array.Empty<Define.BodyPart>());

        // 기본값 백업 (원본 그대로 백업: 해제 시 원 상태로 복원)
        foreach (var b in map.Values)
        {
            b.defaultSprite = b.renderer.sprite;
            b.defaultLocalPos = b.renderer.transform.localPosition;
            b.defaultLocalScale = b.renderer.transform.localScale;
            b.defaultSortingOrder = b.renderer.sortingOrder;
            b.defaultEnabled = b.renderer.enabled;
            b.defaultMaskInteraction = b.renderer.maskInteraction;
            b.defaultMaskEnabled = b.optionalMask ? b.optionalMask.enabled : false;
        }
    }

    void OnDestroy()
    {
        if (eq) eq.OnEquippedChanged -= OnEquippedChanged;
    }

    void OnEquippedChanged(EquipmentSlot slot, EquipmentItemData item)
    {
        RestoreSlot(slot);
        ApplySlot(slot, item);
    }

    private void RestoreSlot(EquipmentSlot slot)
    {
        if (!modifiedBySlot.TryGetValue(slot, out var list) || list == null) return;

        foreach (var part in list)
        {
            if (!map.TryGetValue(part, out var b) || b.renderer == null) continue;

            // 기본으로 복원
            b.renderer.sprite = b.defaultSprite;
            b.renderer.transform.localPosition = b.defaultLocalPos;
            b.renderer.transform.localScale = b.defaultLocalScale;
            b.renderer.sortingOrder = b.defaultSortingOrder;
            b.renderer.enabled = b.defaultEnabled;
            b.renderer.maskInteraction = b.defaultMaskInteraction;

            if (b.optionalMask) b.optionalMask.enabled = b.defaultMaskEnabled;
        }

        list.Clear();
    }

    private void ApplySlot(EquipmentSlot slot, EquipmentItemData item)
    {
        if (!modifiedBySlot.ContainsKey(slot))
            modifiedBySlot[slot] = new List<Define.BodyPart>();
        var modified = modifiedBySlot[slot];

        if (item == null) return;

        // ★ 1) Chest 전용 옵션이 있으면 우선 적용
        if (slot == EquipmentSlot.Chest && item.armor != null && item.armor.enable)
        {
            ApplyChestArmor(item, modified);
            return;
        }

        // 2) 일반 VisualOverride가 있으면 그걸 사용
        if (item.visuals != null && item.visuals.Count > 0)
        {
            foreach (var v in item.visuals)
                ApplyVisualOverride(v, item.icon, modified);
            return;
        }

        // 3) 폴백: slotDefaults에 정의된 파트들에 icon을 꽂기
        if (item.icon != null && defaultMap.TryGetValue(slot, out var targets))
        {
            foreach (var part in targets)
                ApplySpriteToPart(part, item.icon, Vector2.zero, Vector2.one, 0, track: true, modified);
        }
    }

    // --- Chest 전용 적용 ---
    private void ApplyChestArmor(EquipmentItemData item, List<Define.BodyPart> modified)
    {
        // Chest
        Sprite chestSprite = item.armor.chestSprite ? item.armor.chestSprite : item.icon;
        if (chestSprite) // 스프라이트가 있으면만 적용
        {
            ApplySpriteToPart(Define.BodyPart.Chest, chestSprite,
                item.armor.chestOffset, item.armor.chestScale, item.armor.chestSortingOffset,
                track: true, modified);
        }

        // Shoulder L
        if (item.armor.shoulderLeftSprite)
        {
            ApplySpriteToPart(Define.BodyPart.ShoulderL, item.armor.shoulderLeftSprite,
                item.armor.shoulderLOffset, item.armor.shoulderLScale, item.armor.shoulderLSortingOffset,
                track: true, modified);
        }

        // Shoulder R
        if (item.armor.shoulderRightSprite)
        {
            ApplySpriteToPart(Define.BodyPart.ShoulderR, item.armor.shoulderRightSprite,
                item.armor.shoulderROffset, item.armor.shoulderRScale, item.armor.shoulderRSortingOffset,
                track: true, modified);
        }
        else if (item.armor.mirrorRightFromLeft && item.armor.shoulderLeftSprite)
        {
            // 오른쪽을 왼쪽 미러링으로 대체 (scale.x 부호 반전)
            var mirroredScale = new Vector2(-item.armor.shoulderLScale.x, item.armor.shoulderLScale.y);
            ApplySpriteToPart(Define.BodyPart.ShoulderR, item.armor.shoulderLeftSprite,
                item.armor.shoulderROffset, mirroredScale, item.armor.shoulderRSortingOffset,
                track: true, modified);
        }
    }

    // --- 단일 오버라이드 적용 ---
    private void ApplyVisualOverride(VisualOverride v, Sprite iconFallback, List<Define.BodyPart> modified)
    {
        if (!map.TryGetValue(v.part, out var b) || b.renderer == null)
        {
            Debug.LogWarning($"[EVC] Missing PartBinding or SpriteRenderer for {v.part} on {name}");
            return;
        }

        // 스프라이트 결정
        Sprite use = v.sprite;
        if (!use && v.useIconIfEmpty) use = iconFallback;

        if (v.hideRenderer)
        {
            b.renderer.enabled = false;
        }
        else
        {
            b.renderer.enabled = true;
            if (use) b.renderer.sprite = use;
        }

        // 위치/스케일/소팅
        b.renderer.transform.localPosition = b.defaultLocalPos + (Vector3)v.offset;
        {
            var baseScale = BaseScale(b.defaultLocalScale);
            float sx = Mathf.Approximately(v.scale.x, 0f) ? 1f : v.scale.x;
            float sy = Mathf.Approximately(v.scale.y, 0f) ? 1f : v.scale.y;
            b.renderer.transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
        b.renderer.sortingOrder = b.defaultSortingOrder + v.sortingOrderOffset;

        if (v.changeMaskInteraction)
            b.renderer.maskInteraction = v.maskInteraction;
        if (b.optionalMask)
            b.optionalMask.enabled = v.enablePartSpriteMask;

        if (!modified.Contains(v.part)) modified.Add(v.part);
    }

    // --- 공통: 특정 파트에 스프라이트 꽂기 ---
    private void ApplySpriteToPart(Define.BodyPart part, Sprite sprite,
        Vector2 offset, Vector2 scale, int sortingOffset,
        bool track, List<Define.BodyPart> modified)
    {
        if (!map.TryGetValue(part, out var b) || b.renderer == null)
        {
            Debug.LogWarning($"[EVC] Missing PartBinding or SpriteRenderer for {part} on {name}");
            return;
        }
        b.renderer.enabled = true;
        b.renderer.sprite = sprite;

        // 위치/스케일/소팅
        b.renderer.transform.localPosition = b.defaultLocalPos + (Vector3)offset;
        {
            var baseScale = BaseScale(b.defaultLocalScale);
            float sx = Mathf.Approximately(scale.x, 0f) ? 1f : scale.x;
            float sy = Mathf.Approximately(scale.y, 0f) ? 1f : scale.y;
            b.renderer.transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
        b.renderer.sortingOrder = b.defaultSortingOrder + sortingOffset;

        if (track && !modified.Contains(part)) modified.Add(part);
    }
}
