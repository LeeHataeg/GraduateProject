using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[RequireComponent(typeof(StatController))]
public class EquipmentManager : MonoBehaviour
{
    [Header("Refs")]
    public InventorySystem inventory; // 인스펙터 할당 가능 / 런타임 탐색 백업

    private StatController stats;

    // 슬롯별 장착 장비
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
        if (inventory == null)
            inventory = GameManager.Instance?.UIManager?.InventorySys;

        if (inventory == null)
            inventory ??= FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

        if (inventory == null)
            Debug.LogWarning("[EquipmentManager] InventorySystem을 찾지 못했습니다. 장착/해제가 인벤토리에 반영되지 않습니다.");
    }

    public EquipmentItemData GetEquipped(EquipmentSlot slot)
        => equipped.TryGetValue(slot, out var it) ? it : null;

    public bool TryEquip(EquipmentItemData item)
    {
        if (item == null || stats == null)
            return false;

        var slot = item.slot;
        var prev = GetEquipped(slot);

        // 미리 세팅
        equipped[slot] = item;

        // 신규 아이템 적용
        // ※ StatController가 PlayerStatController와 동일한 Apply 시그니처를 갖고 있다고 가정
        stats.Apply(item.modifiers, +1);

        if (prev != null)
        {
            // 기존 아이템 해제(롤백 안전장치 포함)
            if (inventory != null && !inventory.AddItem(prev, 1))
            {
                // 인벤이 꽉 차서 롤백
                stats.Apply(item.modifiers, -1);
                equipped[slot] = prev;
                stats.Apply(prev.modifiers, +1);

                Debug.LogWarning($"[Equip] Equip rollback: no inventory space for {prev.name} (slot={slot}) on {gameObject.name}");
                return false;
            }

            // 기존 장비의 스탯 효과 제거
            stats.Apply(prev.modifiers, -1);
        }

        OnEquippedChanged(slot, item);
        return true;
    }

    public bool TryUnequip(EquipmentSlot slot)
    {
        if (stats == null)
            return false;

        var cur = GetEquipped(slot);
        if (cur == null) return false;

        // 인벤으로 반환 가능해야 해제 진행
        if (inventory != null && !inventory.AddItem(cur, 1))
        {
            Debug.LogWarning($"[Equip] Unequip failed: no inventory space for {cur.name} (slot={slot}) on {gameObject.name}");
            return false;
        }

        // 스탯 제거
        stats.Apply(cur.modifiers, -1);

        // 맵에서 제거
        if (equipped.ContainsKey(slot))
            equipped.Remove(slot);

        OnEquippedChanged(slot, null);
        return true;
    }
}
