using UnityEngine;
using static UnityEditor.Progress;


[RequireComponent(typeof(Collider2D))]
public class ItemPickUp : MonoBehaviour
{
    [Tooltip("줍을 아이템 데이터(SO)")]
    public ItemData itemData;
    [Tooltip("획득 수량")]
    public int quantity = 1;

    private InventorySystem inventory;
    private SpriteRenderer icon;

    private void Awake()
    {
        // 런타임에 InventorySystem 찾기
        inventory = Object.FindFirstObjectByType<InventorySystem>();
        if (inventory == null)
            Debug.LogError("ItemPickup: InventorySystem이 씬에 없습니다!");

        icon = gameObject.GetComponent<SpriteRenderer>();
    }

    public void SetSprite()
    {
        if (icon == null)
            Debug.Log("SpriteRenderer null");
        if (itemData == null)
            Debug.Log("ItemData null");

        icon.sprite = itemData.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;


        var inv = GameManager.Instance?.UIManager?.InventorySys;
        if (inv == null)
        {
            Debug.LogWarning("[ItemPickup] InventorySys가 아직 바인딩되지 않았습니다.");
            return;
        }

        bool added = inv.AddItem(itemData); // ← InventorySystem의 API에 맞춰 호출명 변경
        if (added)
        {
            // UI 즉시 반영 (인벤이 열려 있지 않으면 생략해도 됨)
            GameManager.Instance.UIManager?.SendMessage("TurnOnorOffInven", SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.UIManager?.SendMessage("TurnOnorOffInven", SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject);
        }
        else
        {
            // 꽉 찼거나 조건 불만족
            Debug.Log("[ItemPickup] 인벤토리에 여유가 없습니다.");
        }
    }
}
