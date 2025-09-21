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
    private void Awake()
    {
        // ✅ 자신을 GameManager에 등록(초기화 순서 안정성 ↑)
        GameManager.Instance?.RegisterUIManager(this);
    }

    public void TurnOnorOffInven()
    {
        if (!itemPanel) return;

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
}
