using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[RequireComponent(typeof(StatController))]
public class EquipmentManager : MonoBehaviour
{
    [Header("Refs")]
    public InventorySystem Inventory; // 인스펙터에서 할당

    private StatController stats;
    
    // 각 슬롯과 장착 부위 매핑
    [SerializeField]
    private Dictionary<EquipmentSlot, EquipmentItemData> equipped = new();

    // 외부 UI/뷰가 구독하는 이벤트 (null 방지)
    public event Action<EquipmentSlot, EquipmentItemData> OnEquippedChanged = delegate { };

    private void Awake()
    {
        // PlayerStatController → StatController
        stats = GetComponent<StatController>();
        if (stats == null)
            Debug.LogError("[EquipmentManager] StatController가 필요합니다.");
    }

    private void Start()
    {
        // 1순위: 인스펙터에 수동 연결
        // 2순위: UIManager가 들고 있는 InventorySys
        // 3순위: 씬 전체에서 검색(비활성 포함)
        if (Inventory == null)
            Inventory = GameManager.Instance?.UIManager?.InventorySys;

        if (Inventory == null)
            Inventory ??= FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

        if (Inventory == null)
            Debug.LogWarning("[EquipmentManager] InventorySystem을 찾지 못했습니다. 장착/해제가 인벤토리에 반영되지 않습니다.");
    }

    public EquipmentItemData GetEquipped(EquipmentSlot slot)
        => equipped.TryGetValue(slot, out var it) ? it : null;

    public bool TryEquip(EquipmentItemData item, out EquipmentItemData prevOut)
    {
        prevOut = null;
        if (item == null || stats == null) return false;

        var slot = item.slot;
        var prev = GetEquipped(slot);

        // 옛날거 제거(인벤토리로 복귀), 새삥 장착
        if (prev != null)
            stats.Apply(prev.modifiers, -1);

        stats.Apply(item.modifiers, +1);
        equipped[slot] = item;

        prevOut = prev;

        OnEquippedChanged(slot, item);
        return true;
    }

    public bool TryUnequip(EquipmentSlot slot, out EquipmentItemData removed)
    {
        removed = null;
        if (stats == null) return false;

        var cur = GetEquipped(slot);
        if (cur == null) return false;

        // 스탯 제거
        stats.Apply(cur.modifiers, -1);

        // 맵에서 제거
        equipped.Remove(slot);
        removed = cur;

        OnEquippedChanged(slot, null);
        return true;
    }
}
