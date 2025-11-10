using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("최대 슬롯 개수")] public int capacity = 20;
    [Tooltip("현재 슬롯 목록")] public List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnInventoryChanged;

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

    public bool CanAdd(ItemData item, int quantity = 1)
    {
        int free = 0;
        if (item.maxStack > 1)
        {
            foreach (var s in slots)
            {
                if (s.item == item) free += (item.maxStack - s.quantity);
                if (free >= quantity) return true;
            }
        }
        int empty = capacity - slots.Count;
        return (free + empty) >= quantity;
    }

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

    public bool RemoveAt(int index, int quantity = 1)
    {
        if (index < 0 || index >= slots.Count) return false;

        var s = slots[index];
        if (quantity >= s.quantity) slots.RemoveAt(index);
        else s.quantity -= quantity;

        OnInventoryChanged?.Invoke();
        return true;
    }

    // ★ 추가: 전체 초기화(죽고 재시작 시 런 아이템을 날림)
    public void ClearAll()
    {
        slots.Clear();
        OnInventoryChanged?.Invoke();
#if UNITY_EDITOR
        Debug.Log("[Inventory] Cleared all items.");
#endif
    }
}
