using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Popup & Slot Prefab")]
    public RectTransform popupPanel;
    public Image popupIcon;
    public TextMeshProUGUI popupName;
    public TextMeshProUGUI popupDesc;

    [Header("Slot")]
    public Transform slotContainer;     // Grid/VerticalLayout 등
    public GameObject slotPrefab;       // 반드시 InventorySlotUI 포함된 프리팹

    private InventorySystem inventory;

    void Awake()
    {
        // 가장 확실한 공용 인벤을 먼저 시도 → 없으면 씬에서 검색
        inventory = GameManager.Instance?.UIManager?.InventorySys
                    ?? Object.FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

        if (inventory != null) inventory.OnInventoryChanged += RefreshUI;
        else Debug.LogWarning("[InventoryUI] InventorySystem을 아직 못 찾음. 이후 SetInventory에서 연결 예정.", this);

        if (popupPanel) popupPanel.gameObject.SetActive(false);
    }

    void Start()
    {
        RefreshUI(); // 첫 표시
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= RefreshUI;
    }

    public InventorySystem GetInventoryUnsafe() => inventory; // 슬롯이 참조할 수 있도록 제공

    private void EnsureSlots()
    {
        if (slotContainer == null || slotPrefab == null || inventory == null) return;

        int need = inventory.capacity;
        int have = slotContainer.childCount;

        for (int i = have; i < need; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            go.name = $"Slot_{i:00}";

            // ★ 여기서 같은 inventory를 슬롯에 주입
            var ui = go.GetComponent<InventorySlotUI>();
            if (ui != null) ui.Bind(this, inventory);

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.raycastTarget = true;
            if (img.sprite == null) img.color = new Color(1, 1, 1, 0.001f);
        }
    }

    public void RefreshUI()
    {
        if (slotContainer == null) { Debug.LogWarning("[InventoryUI] slotContainer 미할당", this); return; }
        if (inventory == null) return;

        EnsureSlots();

        var slotUIs = slotContainer.GetComponentsInChildren<InventorySlotUI>(includeInactive: true);
        int uiCount = slotUIs.Length;
        int dataCount = inventory.slots.Count;

        for (int i = 0; i < uiCount; i++)
        {
            // ★ 혹시라도 동적으로 바뀐 인벤 연결을 보정
            if (slotUIs[i] != null) slotUIs[i].Bind(this, inventory);

            if (i < dataCount) slotUIs[i].SetData(inventory.slots[i], i);
            else slotUIs[i].SetEmpty();
        }
    }
    // Tooltip
    public void ShowPopup(InventorySlot slot, Vector2 screenPos)
    {
        if (slot.item == null || popupPanel == null) return;
        popupIcon.sprite = slot.item.icon;
        popupName.text = slot.item.itemName;
        popupDesc.text = slot.item.description;
        popupPanel.gameObject.SetActive(true);
        popupPanel.position = screenPos + new Vector2(popupPanel.rect.width * 0.5f, -popupPanel.rect.height * 0.5f);
    }

    public void SetInventory(InventorySystem sys)
    {
        if (inventory != null) inventory.OnInventoryChanged -= RefreshUI;
        inventory = sys;
        if (inventory != null) inventory.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    public void HidePopup()
    {
        if (popupPanel) popupPanel.gameObject.SetActive(false);
    }
}
