using UnityEngine;
using static Define;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    public EquipmentSlot slot;
    public Image icon;

    private EquipmentUI owner;

    // ★ 부모에서 자동 바인딩 (실수로 Bind 안 불러도 안전)
    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<EquipmentUI>();
    }

    public void Bind(EquipmentUI owner) => this.owner = owner;

    public void Refresh(EquipmentItemData item)
    {
        if (icon == null) return; // 안전 가드

        if (item)
        {
            icon.enabled = true;
            icon.sprite = item.icon;
        }
        else
        {
            icon.enabled = false;
            icon.sprite = null;
        }
    }

    // 우클릭하면 장비 해제
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;

        if (owner != null)
        {
            owner.RequestUnequip(slot);
        }
        else
        {
            // 마지막 방어선: 그 자리에서 부모 탐색 후 재시도
            Debug.LogWarning($"[EquipmentSlotUI] owner is null on {name}. Auto-binding now.");
            owner = GetComponentInParent<EquipmentUI>();
            if (owner != null) owner.RequestUnequip(slot);
        }
    }
}
