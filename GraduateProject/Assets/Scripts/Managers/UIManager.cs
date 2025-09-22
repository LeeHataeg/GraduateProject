using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Item&Inventory")]
    public InventorySystem InventorySys;
    [SerializeField] private InventoryUI invenPanel;
    [SerializeField] private GameObject itemPanel;

    [Header("Death Popup")]
    [SerializeField] private DeathPopupUI deathPopup;    // InGameScene Canvas 안의 팝업

    public DeathPopupUI DeathPopup => deathPopup;

    private bool isTurnedOnInven = false;

    [SerializeField] private EquipmentUI equipmentPanel;

    private void Awake()
    {
        // ✅ 자신을 GameManager에 등록(초기화 순서 안정성 ↑)
        GameManager.Instance?.RegisterUIManager(this);

        if (invenPanel && InventorySys)
            invenPanel.SetInventory(InventorySys);
    }

    private void OnEnable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null)
            pm.OnEquipmentReady += HandlePlayerEquipmentReady; // ★ Player가 준비되면 호출
    }

    private void OnDisable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null)
            pm.OnEquipmentReady -= HandlePlayerEquipmentReady; // ★ 해제
    }

    // ★ PlayerManager.OnEquipmentReady가 호출되면 여기로 들어옴
    private void HandlePlayerEquipmentReady(EquipmentManager eq)
    {
        // 1) 씬 오브젝트 자동 탐색(비어있다면만)
        if (InventorySys == null)
            InventorySys = FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);
        if (invenPanel == null)
            invenPanel = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (equipmentPanel == null)
            equipmentPanel = FindFirstObjectByType<EquipmentUI>(FindObjectsInactive.Include);

        // 2) EquipmentManager ↔ InventorySystem 연결 (장비/인벤 동기화에 필요)
        if (eq != null && eq.inventory == null)
            eq.inventory = InventorySys;

        // 3) EquipmentUI에 방금 스폰된 eq 바인딩
        if (equipmentPanel != null)
        {
            equipmentPanel.eq = eq;
            equipmentPanel.RefreshAll();
        }

        // 4) 인벤토리 UI 즉시 갱신
        invenPanel?.RefreshUI();

#if UNITY_EDITOR
        Debug.Log(
            $"[UIManager] OnEquipmentReady handled. " +
            $"inv={(InventorySys != null ? InventorySys.GetInstanceID() : 0)}, " +
            $"eq={(eq != null ? eq.GetInstanceID() : 0)}"
        );
#endif
    }


    public void TurnOnorOffInven()
    {
        //if (!itemPanel) return;

        isTurnedOnInven = !isTurnedOnInven;
        itemPanel.gameObject.SetActive(isTurnedOnInven);

        if (isTurnedOnInven)
        {
            invenPanel.RefreshUI();
        }
        else
        {
            invenPanel.HidePopup();
        }
    }

    public void ShowDeathPopup()
    {
        if (!deathPopup) return;              // ✅ 널-세이프 no-op
        deathPopup.Show();
    }

    // ★★ 씬 전환 후, InGameScene의 실제 UI들과 연결해주는 진입점
    public void BindSceneInventory(InventorySystem sys, InventoryUI panel, GameObject panelGO)
    {
        InventorySys = sys;
        invenPanel = panel;
        itemPanel = panelGO;

        if (invenPanel != null && InventorySys != null)
        {
            // InventoryUI가 인벤 데이터(InventorySystem)를 참조하도록 연결
            invenPanel.SetInventory(InventorySys);   // ← InventoryUI에 이 메서드가 없다면 public 필드로 직접 할당
            invenPanel.RefreshUI();
        }

        // 아이템 팝업 패널은 기본 비활성화
        if (itemPanel != null) itemPanel.SetActive(false);

#if UNITY_EDITOR
        Debug.Log("[UIManager] BindSceneInventory done.", this);
#endif
    }
}
