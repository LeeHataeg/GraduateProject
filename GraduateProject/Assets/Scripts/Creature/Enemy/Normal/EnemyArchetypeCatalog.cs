using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

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

    // ★ 추가: 이 카탈로그가 적용될 스테이지(1,2,3..)
    [Header("Stage Filter")]
    [Tooltip("이 카탈로그를 적용할 스테이지 번호(예: 1=언데드, 2=오크)")]
    public int stage = 1;

    [Header("후보군")]
    public List<Entry> entries = new();
}
