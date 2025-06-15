using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("최대 슬롯 개수")] public int capacity = 20;
    [Tooltip("현재 슬롯 목록")] public List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnInventoryChanged;

    /// <summary>
    /// 아이템을 인벤토리에 추가합니다.
    /// 스택 가능한 아이템은 기존 슬롯에 합칩니다.
    /// </summary>
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item.maxStack > 1)
        {
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                if (slots[i].item == item && slots[i].quantity < item.maxStack)
                {
                    int space = item.maxStack - slots[i].quantity;
                    int add = Mathf.Min(space, quantity);
                    slots[i].quantity += add;
                    quantity -= add;
                }
            }
        }

        while (quantity > 0)
        {
            if (slots.Count >= capacity)
            {
                OnInventoryChanged?.Invoke();
                return false;
            }
            int add = (item.maxStack > 1) ? Mathf.Min(item.maxStack, quantity) : 1;
            slots.Add(new InventorySlot(item, add));
            quantity -= add;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 인벤토리에서 아이템을 제거합니다.
    /// 수량만큼 빼고 0이 되면 슬롯을 제거합니다.
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        for (int i = slots.Count - 1; i >= 0 && quantity > 0; i--)
        {
            if (slots[i].item == item)
            {
                if (slots[i].quantity > quantity)
                {
                    slots[i].quantity -= quantity;
                    quantity = 0;
                }
                else
                {
                    quantity -= slots[i].quantity;
                    slots.RemoveAt(i);
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return quantity <= 0;
    }

    /// <summary>
    /// 특정 아이템의 총 수량을 조회합니다.
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.item == item)
                count += slot.quantity;
        }
        return count;
    }
}

