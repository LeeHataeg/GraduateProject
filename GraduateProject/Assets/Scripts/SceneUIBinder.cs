// SceneUIBinder.cs (새 파일)
using UnityEngine;

public class SceneUIBinder : MonoBehaviour
{
    [Header("선택: 수동 할당 없으면 자동 탐색")]
    [SerializeField] private InventorySystem inventorySys;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private GameObject itemPanel;

    private void Awake()
    {
        // 자동 탐색(이름/컴포넌트 기준) — 필요에 맞게 바꿔도 됨
        if (!inventorySys) inventorySys = FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);
        if (!inventoryUI) inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (!itemPanel)
        {
            var go = GameObject.Find("ItemPanel");
            if (go) itemPanel = go;
        }

        var uiMgr = GameManager.Instance?.UIManager;
        if (uiMgr == null)
        {
            Debug.LogError("[SceneUIBinder] GameManager.UIManager가 없습니다.");
            return;
        }

        if (!inventorySys || !inventoryUI || !itemPanel)
        {
            Debug.LogError("[SceneUIBinder] 씬 UI 참조를 일부 찾지 못했습니다. 수동 할당하거나, 이름을 확인하세요.");
            return;
        }

        uiMgr.BindSceneInventory(inventorySys, inventoryUI, itemPanel);
    }
}
