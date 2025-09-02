using System.Collections.Generic;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Inventory/Equipment Item")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Info")]
    public EquipmentSlot slot;
    public List<StatModifier> modifiers = new();
}
