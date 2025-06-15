using UnityEngine;

public enum ItemType { Consumable, Equipment, Quest, Misc }

// ScriptableObject로 아이템 데이터를 정의
[CreateAssetMenu(menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Tooltip("아이템 이름")] public string itemName;
    [Tooltip("아이템 아이콘")] public Sprite icon;
    [Tooltip("아이템 타입")] public ItemType itemType;
    [Tooltip("최대 스택 수")] public int maxStack = 1;
    [TextArea, Tooltip("아이템 설명")] public string description;
}
