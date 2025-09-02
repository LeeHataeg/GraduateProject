using UnityEngine;
using static Define;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    public EquipmentSlot slot;
    public Image icon;

    private EquipmentUI owner;

    public void Bind(EquipmentUI owner) => this.owner = owner;

    public void Refresh(EquipmentItemData item)
    {
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
        if (e.button == PointerEventData.InputButton.Right)
            owner.RequestUnequip(slot);
    }
}