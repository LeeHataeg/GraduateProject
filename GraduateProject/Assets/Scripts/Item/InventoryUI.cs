using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Popup & Slot Prefab")]
    public RectTransform popupPanel;
    public Image popupIcon;         // 팝업 안의 아이콘 Image
    public TextMeshProUGUI popupName;    // 팝업 안의 아이템 이름
    public TextMeshProUGUI popupDesc;    // 팝업 안의 설명

    [Header("Slot")]
    public Transform slotContainer;  // SlotContainer 참조
    public GameObject slotPrefab;     // InventorySlotPrefab

    private InventorySystem inventory;

    private void Awake()
    {
        inventory = Object.FindFirstObjectByType<InventorySystem>();
        if (inventory == null)
            Debug.LogError("[InventoryUI] InventorySystem 인스턴스를 찾을 수 없습니다.");
        else
            inventory.OnInventoryChanged += RefreshUI;

        popupPanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        var slotUIs = slotContainer.GetComponentsInChildren<InventorySlotUI>(includeInactive: true);

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (i < inventory.slots.Count)
                slotUIs[i].SetData(inventory.slots[i], i); // 인덱스 전달!
            else
                slotUIs[i].SetEmpty();
        }
    }

    // 마우스 올렸을 때 호출
    public void ShowPopup(InventorySlot slot, Vector2 screenPos)
    {
        if (slot.item == null) return;

        popupIcon.sprite = slot.item.icon;
        popupName.text = slot.item.itemName;
        popupDesc.text = slot.item.description;
        popupPanel.gameObject.SetActive(true);

        // 마우스 위치 기준으로 살짝 오프셋
        popupPanel.position = screenPos + new Vector2(popupPanel.rect.width * 0.5f, -popupPanel.rect.height * 0.5f);
    }

    // 마우스 나갔을 때 호출
    public void HidePopup()
    {
        popupPanel.gameObject.SetActive(false);
    }
}
