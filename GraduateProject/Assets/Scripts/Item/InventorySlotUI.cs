using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 UI 하나를 담당할 컴포넌트
public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    private InventorySlot slotData;

    // 슬롯 데이터를 할당하고 UI 갱신
    public void SetData(InventorySlot slot)
    {
        slotData = slot;
        if (slotData.item != null)
        {
            iconImage.sprite = slotData.item.icon;
            iconImage.enabled = true;
            quantityText.text = slotData.quantity > 1
                                        ? slotData.quantity.ToString()
                                        : string.Empty;
        }
        else
        {
            iconImage.enabled = false;
            quantityText.text = string.Empty;
        }
    }

    public void SetEmpty()
    {
        iconImage.enabled = false;
        quantityText.text = string.Empty;
    }
}