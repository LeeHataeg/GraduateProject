using UnityEngine;

public class UIManager : MonoBehaviour
{
    public InventorySystem InventorySys;
    [SerializeField] GameObject InventoryPanel;
    bool isTurnedOnInven = false;
    public void TurnOnorOffInven()
    {
        isTurnedOnInven = !isTurnedOnInven;
        InventoryPanel.SetActive(isTurnedOnInven);

        if (isTurnedOnInven)
        {
            var ui = InventoryPanel.GetComponent<InventoryUI>();
            ui?.RefreshUI();
        }
    }
}
