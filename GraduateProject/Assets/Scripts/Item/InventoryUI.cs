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
        // 인게임씬에서만 있을 수 있으므로 null이면 그냥 대기(에러 말고 warning)
        inventory = Object.FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshUI;
        }
        else
        {
            Debug.LogWarning("[InventoryUI] InventorySystem을 아직 못 찾음. 이후 SetInventory에서 연결 예상.", this);
        }

        if (popupPanel) popupPanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (slotContainer == null) { Debug.LogWarning("[InventoryUI] slotContainer 미할당", this); return; }
        if (inventory == null) { /*아직 인벤 연결 전*/ return; }

        var slotUIs = slotContainer.GetComponentsInChildren<InventorySlotUI>(includeInactive: true);
        var count = slotUIs.Length;

        for (int i = 0; i < count; i++)
        {
            if (i < inventory.slots.Count)
                slotUIs[i].SetData(inventory.slots[i], i);
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

    public void SetInventory(InventorySystem sys)
    {
        // 이전 구독 해제
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        inventory = sys;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

}
