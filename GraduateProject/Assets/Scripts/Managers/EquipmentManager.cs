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

    public event Action<EquipmentSlot, EquipmentItemData> OnEquippedChanged = delegate { }; // ✅ null 방지
   

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
        var prev = GetEquipped(slot);

        equipped[slot] = item;
        stats.Apply(item.modifiers, +1);

        if (prev != null)
        {
            stats.Apply(prev.modifiers, -1);
            if (!inventory.AddItem(prev, 1))
            {
                // 롤백
                stats.Apply(item.modifiers, -1);
                equipped[slot] = prev;
                stats.Apply(prev.modifiers, +1);
                return false;
            }
        }

        Debug.Log($"[Equip] {item.name} slot={slot} (eq on {gameObject.name})");
        OnEquippedChanged(slot, item); // ✅ 한 번만 호출
        return true;
    }

    public bool TryUnequip(EquipmentSlot slot)
    {
        var cur = GetEquipped(slot);
        if (cur == null) return false;

        if (!inventory.AddItem(cur, 1))
        {
            Debug.LogWarning($"[Equip] Unequip failed: no inventory space for {cur.name} (slot={slot}) on {gameObject.name}");
            return false;
        }

        stats.Apply(cur.modifiers, -1);

        if (equipped.ContainsKey(slot))
            equipped.Remove(slot); // ✅ null 세팅 대신 제거로 깔끔

        Debug.Log($"[Equip] Unequip {cur.name} from slot={slot} (eq on {gameObject.name})");
        OnEquippedChanged(slot, null); // ✅ 해제 알림
        return true;
    }
}
