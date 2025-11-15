using System.Collections.Generic;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Inventory/Equipment Item")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Info")]
    public EquipmentSlot slot;
    public List<StatModifier> modifiers = new List<StatModifier>();

    [Header("Visual Overrides (optional)")]
    public List<VisualOverride> visuals = new();

    [Header("Armor (Chest) Options")]
    public ArmorVisualOptions armor;


    [Header("무기만")]
    public WeaponType WeaponType;

    [Header("원거리 무기만")]
    [Tooltip("이 변수 누름? 고럼 원거리임 ㅇㅇ.")]
    public bool IsRanged;
    public GameObject Bullet;

    // 탄창
    [Tooltip("탄창 최대 몇?")]
    public int MagMaxCount;     // 탄창 크기
    public float BulletSpeed;
    public float BulletLifeTime;

    // 재장전
    [Tooltip("장전시간")]
    public float ReloadTime;

    public float atkCoolTime;
}
