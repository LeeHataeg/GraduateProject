using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Popup & Slot Prefab")]
    public RectTransform popupPanel;
    public Transform slotContainer;  // SlotContainer 참조
    public GameObject slotPrefab;     // InventorySlotPrefab

    [Header("Size Ratios")]
    [Range(0.1f, 1f)] public float widthRatio;
    [Range(0.1f, 1f)] public float heightRatio;

    private InventorySystem inventory;

    private void Awake()
    {
        inventory = Object.FindFirstObjectByType<InventorySystem>();
        if (inventory == null)
            Debug.LogError("[InventoryUI] InventorySystem 인스턴스를 찾을 수 없습니다.");
        else
            inventory.OnInventoryChanged += RefreshUI;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        // 1) SlotContainer에 에디터에서 미리 배치해 둔 InventorySlotPrefab들 가져오기
        var slotUIs = slotContainer.GetComponentsInChildren<InventorySlotUI>(includeInactive: true);

        for (int i = 0; i < slotUIs.Length; i++)
        {
            //
            if (i < inventory.slots.Count)
                slotUIs[i].SetData(inventory.slots[i]);
            else
                slotUIs[i].SetEmpty();
        }
    }
}
