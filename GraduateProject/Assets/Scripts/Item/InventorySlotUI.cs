using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    private InventorySlot slotData;
    private int slotIndex = -1;
    private float lastClickTime = -999f;
    private const float DOUBLE_CLICK = 0.25f;

    private InventorySystem inventory;
    private EquipmentManager equipment;

    void Awake()
    {
        inventory = FindFirstObjectByType<InventorySystem>();
        equipment = FindFirstObjectByType<EquipmentManager>();
    }

    // 인덱스 포함 버전
    public void SetData(InventorySlot slot, int index)
    {
        slotData = slot;
        slotIndex = index;

        if (slotData?.item != null)
        {
            iconImage.sprite = slotData.item.icon;
            iconImage.enabled = true;
            quantityText.text = slotData.quantity > 1 ? slotData.quantity.ToString() : string.Empty;
        }
        else
        {
            SetEmpty();
        }
    }

    public void SetEmpty()
    {
        slotData = null;
        slotIndex = -1;
        iconImage.enabled = false;
        quantityText.text = string.Empty;
    }

    public void OnPointerClick(PointerEventData e)
    {
        // 좌클릭만 처리
        if (e.button != PointerEventData.InputButton.Left) return;
        if (slotData == null || slotData.item == null) return;

        // 더블클릭 판정
        float now = Time.unscaledTime;
        bool isDouble = (now - lastClickTime) <= DOUBLE_CLICK;
        lastClickTime = now;
        if (!isDouble) return;

        // 장비 아이템만 처리
        if (!(slotData.item is EquipmentItemData eqData))
        {
            Debug.Log($"[INV] dbl-click ignored: not equipment ({slotData.item.name})");
            return;
        }

        // 런타임 Player의 Unit Root에서 EquipmentManager 획득
        if (equipment == null)
        {
            var player = GameManager.Instance?.PlayerManager?.Player;
            if (player != null)
                equipment = player.GetComponentInChildren<EquipmentManager>(true);
        }

        if (equipment == null) return;

        // 장착 성공 시 해당 인벤토리 슬롯에서 정확히 1개 제거
        if (equipment.TryEquip(eqData))
            inventory.RemoveAt(slotIndex, 1);
    }
}
