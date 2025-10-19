using UnityEngine;

public static class EchoInventoryBridge
{
    /// 아이템 ‘사용’ 시점에 1줄 호출: EchoRecorder에 기록
    public static void RaiseItemUsed(GameObject playerGO, string itemId)
    {
        var rec = playerGO.GetComponent<EchoRecorder>();
        rec?.MarkItemUsed(itemId);
    }

    /// 스태시 아이템 지급(보스전 시작 때 1회) → 너의 인벤토리 API로 교체
    public static void TryGiveItemToPlayer(PlayerController player, string itemId)
    {
        // 예: player.Inventory.AddItemById(itemId);
        Debug.Log($"[Echo] Grant stash item: {itemId}");
    }
}
