using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 슬롯 UI 하나를 담당할 컴포넌트
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    private InventorySlot slotData;
    private InventoryUI inventoryUI;// ???

    private void Awake()
    {
        inventoryUI = gameObject.GetComponentInParent<InventoryUI>();
        if (inventoryUI == null)
            Debug.LogError("[InventorySlotUI] InventoryUI를 찾을 수 없습니다.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotData?.item == null) return;
        // 화면 좌표를 넘겨서 팝업 위치를 잡도록
        inventoryUI.ShowPopup(slotData, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.HidePopup();
    }

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
        slotData = null;
        iconImage.enabled = false;
        quantityText.text = string.Empty;
    }
}