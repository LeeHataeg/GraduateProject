using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Item&Inventory")]
    public InventorySystem InventorySys;
    [SerializeField] private InventoryUI invenPanel;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private EquipmentUI equipmentPanel;

    [SerializeField] private MagCountUI magCountPanel;

    [Header("HpUI")]
    [SerializeField] private CharacteCurStateUI playerHp;
    [SerializeField] private CharacteCurStateUI bossHp;

    [Header("Death Popup")]
    [SerializeField] private DeathPopupUI deathPopup;    // InGameScene Canvas 안의 팝업
    public DeathPopupUI DeathPopup => deathPopup;

    [Header("Ranking UI")]
    [SerializeField] private RankingUI rankingPanel;

    [Header("Clear Popup")]
    [SerializeField] private GameObject ClearPanel;

    private bool isTurnedOnInven = false;

    [Header("Minimap")]
    [SerializeField] private GameObject minimap;
    private void Awake()
    {
        // 프로젝트에 따라 GameManager에 등록 메서드가 없을 수도 있으니, 예외 없이 시도만 함
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetUIManger(this);
        }

        if (invenPanel && InventorySys)
            invenPanel.SetInventory(InventorySys);
    }

    private void Start()
    {
        if (InventorySys == null)
            InventorySys = FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

        if (invenPanel && InventorySys)
            invenPanel.SetInventory(InventorySys);
    }

    public void SetInven(InventorySystem inven)
    {
        this.InventorySys = inven;
        if (invenPanel != null)
            invenPanel.SetInventory(inven);
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

    public void ShowRankingOrClearPanel(int stageIndex)
    {
        // RankingUI가 세팅되어 있으면 → 랭킹 먼저
        Debug.Log("[UIManager] - ShowRank");

        if (rankingPanel != null)
        {
            Debug.Log("[UIManager] - rankingPanel이 null 아니에용");

            int lastScore = 0;

            var rm = FindFirstObjectByType<RankingManager>(FindObjectsInactive.Include);
            if (rm != null)
            {
                var rec = rm.GetRecord(stageIndex);
                if (rec != null)
                    lastScore = rec.lastScore;
            }

            // ClearPanel은 RankingUI 인스펙터에서 nextPanel로 연결해 둔다.
            Debug.Log("[UIManager] - OpenForStage호출해용");
            rankingPanel.OpenForStage(stageIndex, lastScore);
        }
        else
        {
            // 랭킹 UI 없으면 기존처럼 바로 클리어 패널
            Debug.Log("[UIManager] - rankingPanel이 null 이에용");

            if (stageIndex <= GameManager.Instance.Stages.Count)
                ShowClearPanel();
        }
    }


    private void HandlePlayerEquipmentReady(EquipmentManager eq)
    {
        // 1) 씬 오브젝트 자동 탐색(비어있다면만)
        if (InventorySys == null)
            InventorySys = FindFirstObjectByType<InventorySystem>();
        if (invenPanel == null)
            invenPanel = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (equipmentPanel == null)
            equipmentPanel = FindFirstObjectByType<EquipmentUI>(FindObjectsInactive.Include);

        // 2) EquipmentManager ↔ InventorySystem 연결 (장비/인벤 동기화에 필요)
        if (eq != null && eq.Inventory == null)
            eq.Inventory = InventorySys;

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

    public void TurnOnOrOffMagUI(bool on) => magCountPanel.SetActivation(on);

    public void SetRangedMagUI(int max, int cur) => magCountPanel.SetRanged(max, cur);

    public void SetCurMagUI(int cur) => magCountPanel.SetCurMagCount(cur);

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

    public void SetHpUI(HealthController hp)
    {
        playerHp.SetTarget(hp);
    }
    public void SetHpUI(HealthController hp, string name)
    {
        bossHp.SetTarget(hp);
        bossHp.SetTargetName(name);
    }

    public void SetActiveBossHpUI(bool value)
    {
        // bossHp 컴포넌트 자체가 이미 Destroy 되었으면 그냥 리턴
        if (bossHp == null)
            return;

        // 혹시 모를 안전빵 (보통 여기까지 오면 gameObject도 같이 죽어있긴 함)
        var go = bossHp.gameObject;
        if (go == null)
            return;

        go.SetActive(value);
    }

    public void SetActivationMinmap()
    {
        minimap.SetActive(!minimap.activeSelf);
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

        if (magCountPanel != null)
            magCountPanel.gameObject.SetActive(false);

        if(minimap != null)
            minimap.SetActive(false);
#if UNITY_EDITOR
        Debug.Log("[UIManager] HideAll called: all UI panels closed.");
#endif
    }
}
