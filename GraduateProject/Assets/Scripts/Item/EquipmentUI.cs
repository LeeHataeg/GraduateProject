using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EquipmentUI : MonoBehaviour
{
    public EquipmentManager eq;
    public List<EquipmentSlotUI> slots = new();

    private void Awake()
    {
        BindSlots();          // ★ 슬롯들에 this 바인딩
    }

    private void OnEnable()
    {
        // (프로젝트에 따라 있을 수도/없을 수도 있는 이벤트면 그대로 두고, 없으면 주석 처리하세요)
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null) pm.OnEquipmentReady += HandleEqReady;

        TryAttachExisting();
        RefreshAll();
    }

    private void OnDisable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null) pm.OnEquipmentReady -= HandleEqReady;
        if (eq != null) eq.OnEquippedChanged -= OnEquippedChanged;
    }

    private void BindSlots()
    {
        if (slots == null || slots.Count == 0)
            slots = new List<EquipmentSlotUI>(GetComponentsInChildren<EquipmentSlotUI>(true));

        foreach (var s in slots)
            if (s != null) s.Bind(this);   // ★ 확실히 묶어줌
    }

    private void HandleEqReady(EquipmentManager mgr) => AttachEq(mgr);

    private void TryAttachExisting()
    {
        var p = GameManager.Instance?.PlayerManager?.Player;
        if (!p) return;

        var mgr = p.GetComponentInChildren<EquipmentManager>(true);
        if (mgr) AttachEq(mgr);
    }

    private void AttachEq(EquipmentManager mgr)
    {
        if (eq != null) eq.OnEquippedChanged -= OnEquippedChanged;
        eq = mgr;
        if (eq != null) eq.OnEquippedChanged += OnEquippedChanged;
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (!eq) return;
        foreach (var s in slots)
            if (s != null) s.Refresh(eq.GetEquipped(s.slot));
    }

    private void OnEquippedChanged(EquipmentSlot slot, EquipmentItemData item)
    {
        RefreshAll();
    }

    public void RequestUnequip(EquipmentSlot slot)
    {
        if (eq?.TryUnequip(slot) == true)
            RefreshAll();
    }
}
