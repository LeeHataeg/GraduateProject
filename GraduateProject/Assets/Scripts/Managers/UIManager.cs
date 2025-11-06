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

    [SerializeField] private EquipmentUI equipmentPanel;

    [Header("Death Popup")]
    [SerializeField] private GameObject ClearPanel;

    private bool isTurnedOnInven = false;

    private void Awake()
    {
        // 프로젝트에 따라 GameManager에 등록 메서드가 없을 수도 있으니, 예외 없이 시도만 함
        var gm = GameManager.Instance;
        if (gm != null)
        {
            // GameManager에 RegisterUIManager가 있으면 호출(없으면 무시)
            var mi = gm.GetType().GetMethod("RegisterUIManager");
            if (mi != null) mi.Invoke(gm, new object[] { this });
        }

        if (invenPanel && InventorySys)
            invenPanel.SetInventory(InventorySys);
    }

    private void OnEnable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null)
            pm.OnEquipmentReady += HandlePlayerEquipmentReady;
    }

    private void OnDisable()
    {
        var pm = GameManager.Instance?.PlayerManager;
        if (pm != null)
            pm.OnEquipmentReady -= HandlePlayerEquipmentReady;
    }

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
        isTurnedOnInven = !isTurnedOnInven;
        if (itemPanel) itemPanel.gameObject.SetActive(isTurnedOnInven);

        if (isTurnedOnInven)
            invenPanel?.RefreshUI();
        else
            invenPanel?.HidePopup();
    }

    public void ShowClearPanel()
    {
        ClearPanel.SetActive(true);
    }

    public void ShowDeathPopup()
    {
        if (!deathPopup)
            deathPopup = FindFirstObjectByType<DeathPopupUI>(FindObjectsInactive.Include);

        if (!deathPopup)
        {
            Debug.LogWarning("[UIManager] DeathPopupUI를 찾지 못해 패널을 띄울 수 없습니다.", this);
            return;
        }

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
            invenPanel.SetInventory(InventorySys);
            invenPanel.RefreshUI();
        }

        if (itemPanel != null) itemPanel.SetActive(false);

#if UNITY_EDITOR
        Debug.Log("[UIManager] BindSceneInventory done.", this);
#endif
    }

    /// <summary>
    /// 현재 표시 중인 모든 UI를 안전하게 닫는다.
    /// - 인벤토리 패널/툴팁
    /// - 데스 팝업
    /// - (필요 시) 기타 패널을 여기서 추가
    /// </summary>
    public void HideAll()
    {
        // 인벤토리 패널 끄기
        if (itemPanel != null && itemPanel.activeSelf)
            itemPanel.SetActive(false);
        isTurnedOnInven = false;

        // 인벤 툴팁 닫기
        invenPanel?.HidePopup();

        // 데스 팝업 닫기(있을 때만)
        if (deathPopup != null && deathPopup.gameObject.activeSelf)
            deathPopup.Hide();

        if(ClearPanel!= null && ClearPanel.activeSelf)
            ClearPanel.SetActive(false);

#if UNITY_EDITOR
        Debug.Log("[UIManager] HideAll called: all UI panels closed.");
#endif
    }
}
