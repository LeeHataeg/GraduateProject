using UnityEngine;

public class UIManager : MonoBehaviour
{
    public InventorySystem InventorySys;
    [SerializeField] InventoryUI InventoryPanel;
    bool isTurnedOnInven = false;
    public void TurnOnorOffInven()
    {
        isTurnedOnInven = !isTurnedOnInven;
        InventoryPanel.gameObject.SetActive(isTurnedOnInven);

        if (isTurnedOnInven)
        {
            InventoryPanel.RefreshUI();
        }
        else
        {
            InventoryPanel.HidePopup();
        }
    }
}
