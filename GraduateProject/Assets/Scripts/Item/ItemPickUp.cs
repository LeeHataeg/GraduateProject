using UnityEngine;


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

        Debug.Log("Player 만남 ㅋ");

        // 인벤토리에 담고, 성공하면 파괴
        if (inventory.AddItem(itemData, quantity))
        {
            Debug.Log("Item 추가 ㅇㅇ");

            Destroy(gameObject);
        }
        else
        {
            // (선택) 인벤토리 꽉 찼을 때 피드백
            Debug.Log("인벤토리가 가득 찼습니다!");
        }
    }
}
