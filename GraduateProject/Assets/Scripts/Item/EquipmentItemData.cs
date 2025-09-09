using System.Collections.Generic;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Inventory/Equipment Item")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Info")]
    public EquipmentSlot slot;
    public List<StatModifier> modifiers = new();

    [Header("Visual Overrides (optional)")]
    public List<VisualOverride> visuals = new();  // Hat/Hair/무기 등은 기존 방식 유지

    [Header("Armor (Chest) Options")]
    public ArmorVisualOptions armor;              // ★ Chest 전용 옵션
}
