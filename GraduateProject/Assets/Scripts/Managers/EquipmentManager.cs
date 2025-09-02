using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[RequireComponent(typeof(PlayerStatController))]
public class EquipmentManager : MonoBehaviour
{
    public InventorySystem inventory; // 인스펙터 할당 또는 런타임 탐색
    private PlayerStatController stats;

    [SerializeField] private Dictionary<EquipmentSlot, EquipmentItemData> equipped = new();

    public event Action<EquipmentSlot, EquipmentItemData> OnEquippedChanged;

    void Awake()
    {
        stats = GetComponent<PlayerStatController>();
    }

    public EquipmentItemData GetEquipped(EquipmentSlot slot)
        => equipped.TryGetValue(slot, out var it) ? it : null;

    private void Start()
    {
        inventory = GameManager.Instance.UIManager.InventorySys;
    }

    public bool TryEquip(EquipmentItemData item)
    {
        if (item == null) return false;

        var slot = item.slot;
        EquipmentItemData prev = GetEquipped(slot);

        // 인벤토리 자리/수량 체크는 UI 쪽에서 index 단위로 빼줄 예정
        // 여기서는 순수 장착/스왑만
        equipped[slot] = item;
        stats.Apply(item.modifiers, +1);

        if (prev != null)
        {
            // 스왑: 이전 장비 보정 제거 + 인벤토리로 복귀 시도
            stats.Apply(prev.modifiers, -1);
            if (!inventory.AddItem(prev, 1))
            {
                // 실패 시 롤백
                stats.Apply(item.modifiers, -1);
                equipped[slot] = prev;
                stats.Apply(prev.modifiers, +1);
                return false;
            }
        }

        OnEquippedChanged?.Invoke(slot, item);
        return true;
    }

    public bool TryUnequip(EquipmentSlot slot)
    {
        var cur = GetEquipped(slot);
        if (cur == null) return false;

        if (!inventory.AddItem(cur, 1)) return false;
        stats.Apply(cur.modifiers, -1);
        equipped[slot] = null;
        OnEquippedChanged?.Invoke(slot, null);
        return true;
    }
}
