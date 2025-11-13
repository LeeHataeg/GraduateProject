using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("최대 슬롯 개수")] public int MaxItemCount = 20;
    [Tooltip("현재 슬롯 목록")] public List<InventorySlot> Slots = new List<InventorySlot>();

    // 인벤토리 UI를 켤 때 마다 호출되는 이벤트 : 인벤토리 변화(장착, 제거, 추가 등)를 반영하기 위해
    public event Action OnInventoryChanged;

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item.maxStack > 1)
        {
            for (int i = 0; i < Slots.Count && quantity > 0; i++)
            {
                if (Slots[i].item == item && Slots[i].quantity < item.maxStack)
                {
                    int space = item.maxStack - Slots[i].quantity;
                    int add = Mathf.Min(space, quantity);
                    Slots[i].quantity += add;
                    quantity -= add;
                }
            }
        }

        while (quantity > 0)
        {
            if (Slots.Count >= MaxItemCount)
            {
                OnInventoryChanged?.Invoke();
                return false;
            }
            int add = (item.maxStack > 1) ? Mathf.Min(item.maxStack, quantity) : 1;
            Slots.Add(new InventorySlot(item, add));
            quantity -= add;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }
    
    // 아직 스택형 아이템(예를 들어 포혓)이 적용되지 않아 사용되지 않는 함수
    public bool CanAddItem(ItemData item, int quantity = 1)
    {
        int free = 0;
        if (item.maxStack > 1)
        {
            foreach (var s in Slots)
            {
                if (s.item == item) free += (item.maxStack - s.quantity);
                if (free >= quantity) return true;
            }
        }
        int empty = MaxItemCount - Slots.Count;
        return (free + empty) >= quantity;
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        for (int i = Slots.Count - 1; i >= 0 && quantity > 0; i--)
        {
            if (Slots[i].item == item)
            {
                if (Slots[i].quantity > quantity)
                {
                    Slots[i].quantity -= quantity;
                    quantity = 0;
                }
                else
                {
                    quantity -= Slots[i].quantity;
                    Slots.RemoveAt(i);
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return quantity <= 0;
    }

    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (var slot in Slots)
        {
            if (slot.item == item)
                count += slot.quantity;
        }
        return count;
    }

    public bool RemoveAtInventory(int index, int quantity = 1)
    {
        if (index < 0 || index >= Slots.Count) return false;

        var s = Slots[index];
        if (quantity >= s.quantity) Slots.RemoveAt(index);
        else s.quantity -= quantity;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ClearAllItems()
    {
        Slots.Clear();
        OnInventoryChanged?.Invoke();
#if UNITY_EDITOR
        Debug.Log("[Inventory] Cleared all items.");
#endif
    }
}
