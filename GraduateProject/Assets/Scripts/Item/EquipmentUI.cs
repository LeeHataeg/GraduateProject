using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Define;

using UnityEngine;
using System.Collections.Generic;

public class EquipmentUI : MonoBehaviour
{
    public EquipmentManager eq;
    public List<EquipmentSlotUI> slots = new();

    void OnEnable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null) pm.OnEquipmentReady += HandleEqReady;
        TryAttachExisting();
    }
    void OnDisable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null) pm.OnEquipmentReady -= HandleEqReady;
        if (eq != null) eq.OnEquippedChanged -= OnEquippedChanged;
    }
    void HandleEqReady(EquipmentManager mgr) => AttachEq(mgr);
    void TryAttachExisting()
    {
        var p = GameManager.Instance?.PlayerManager?.Player;
        if (!p) return;
        var mgr = p.GetComponentInChildren<EquipmentManager>(true);
        if (mgr) AttachEq(mgr);
    }
    void AttachEq(EquipmentManager mgr)
    {
        if (eq != null) eq.OnEquippedChanged -= OnEquippedChanged;
        eq = mgr;
        eq.OnEquippedChanged += OnEquippedChanged;
        RefreshAll();
    }
    public void RefreshAll()
    {
        if (!eq) return;
        foreach (var s in slots) s.Refresh(eq.GetEquipped(s.slot));
    }
    void OnEquippedChanged(EquipmentSlot slot, EquipmentItemData item) { RefreshAll(); }
    public void RequestUnequip(EquipmentSlot slot)
    {
        if (eq?.TryUnequip(slot) == true)
            RefreshAll();
    }

    //    private void OnEquippedChanged(EquipmentSlot slot, EquipmentItemData item)
    //    {
    //        foreach (var s in slots)
    //            if (s.slot == slot) { s.Refresh(item); break; }
    //    }

    //    // 슬롯 우클릭 해제 요청
    //    public void RequestUnequip(EquipmentSlot slot)
    //    {
    //        if (eq != null && eq.TryUnequip(slot)) RefreshAll();
    //    }
    //}
}