using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [Serializable]
    public struct DropEntry
    {
        public ItemData itemData;
        [Range(0f, 1f)] public float dropChance;  // ex: 0.3 = 30% 확률
        public int minQuantity, maxQuantity;     // 드롭 수량 범위
    }

    [Tooltip("스폰할 ItemPickup 프리팹")]
    public GameObject pickupPrefab;
    [Tooltip("드롭 테이블 설정")]
    public List<DropEntry> dropTable = new List<DropEntry>();

    // 예: IHealth 인터페이스 사용 시
    private IHealth health;

    private void Awake()
    {
        health = GetComponent<IHealth>();
        if (health != null)
            health.OnDead += DropAll;
    }

    private void DropAll()
    {
        Vector3 spawnPos = transform.position;
        foreach (var entry in dropTable)
        {
            if (UnityEngine.Random.value > entry.dropChance)
                continue;

            int qty = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
            var go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
            var pickup = go.GetComponent<ItemPickUp>();
            pickup.itemData = entry.itemData;
            pickup.quantity = qty;
            pickup.SetSprite();
        }
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDead -= DropAll;
    }
}