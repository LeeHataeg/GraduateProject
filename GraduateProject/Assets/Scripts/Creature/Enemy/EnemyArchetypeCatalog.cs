using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Archetype Catalog")]
public class EnemyArchetypeCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EnemyArchetypeSO archetype;
        [Range(0f, 1f)] public float weight = 0.25f;
    }

    [Header("이 카탈로그가 적용될 룸 타입")]
    public RoomType roomType = RoomType.Normal;

    [Header("후보군")]
    public List<Entry> entries = new();
}
