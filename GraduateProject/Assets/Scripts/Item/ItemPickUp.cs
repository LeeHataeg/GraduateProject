using UnityEngine;

[DefaultExecutionOrder(10)]
[RequireComponent(typeof(Collider2D))]
public class ItemPickUp : MonoBehaviour
{
    [Tooltip("줍을 아이템 데이터(SO)")]
    public ItemData itemData;
    [Tooltip("획득 수량")]
    public int quantity = 1;

    private SpriteRenderer icon;
    private InventorySystem inventory;   // ★ 로컬 캐시

    private void Awake()
    {
        icon = GetComponent<SpriteRenderer>();

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log("[ItemPickup] BoxCollider2D added.");
        }

        col.isTrigger = true;
    }
    public void SetSprite()
    {
        if (!icon)
            icon = GetComponent<SpriteRenderer>();

        if (icon == null)
            Debug.LogWarning("[ItemPickup] SpriteRenderer null");
        if (itemData == null)
            Debug.LogWarning("[ItemPickup] ItemData null");

        if (icon != null && itemData != null)
            icon.sprite = itemData.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance.UIManager.InventorySys == null)
        {
            if (inventory == null)
            {
                Debug.Log("[ItemPickup] Not InvenSys (InventorySystem ㄹㅇ 없음)");
                return;
            }
        }

        bool added = GameManager.Instance.UIManager.InventorySys.AddItem(itemData, quantity);
        if (added)
        {
            var ui = GameManager.Instance?.UIManager;
            ui?.SendMessage("TurnOnorOffInven", SendMessageOptions.DontRequireReceiver);
            ui?.SendMessage("TurnOnorOffInven", SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[ItemPickup] Not Capacity in Inven");
        }
    }
}
