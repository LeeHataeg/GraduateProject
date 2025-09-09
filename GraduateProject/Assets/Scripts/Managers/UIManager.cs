using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public InventorySystem InventorySys;

    [SerializeField] private InventoryUI invenPanel;
    [SerializeField] private GameObject itemPanel;

    private bool isTurnedOnInven = false;

    public void TurnOnorOffInven()
    {
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
}
