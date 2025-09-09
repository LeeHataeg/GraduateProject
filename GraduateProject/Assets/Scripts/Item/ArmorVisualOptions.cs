using System;
using UnityEngine;

[Serializable]
public class ArmorVisualOptions
{
    [Tooltip("이 갑옷이 가슴 + 양어깨를 관리할지 여부")]
    public bool enable = false;

    [Header("Chest")]
    public Sprite chestSprite;                    // 비어 있으면 icon 사용 가능
    public Vector2 chestOffset;
    public Vector2 chestScale = Vector2.one;
    public int chestSortingOffset = 0;

    [Header("Shoulder L")]
    public Sprite shoulderLeftSprite;             // 비어 있으면 적용 안 함
    public Vector2 shoulderLOffset;
    public Vector2 shoulderLScale = Vector2.one;
    public int shoulderLSortingOffset = 0;

    [Header("Shoulder R")]
    public Sprite shoulderRightSprite;            // 비어있고 mirrorRightFromLeft=true면 L을 미러링
    public bool mirrorRightFromLeft = false;
    public Vector2 shoulderROffset;
    public Vector2 shoulderRScale = Vector2.one;
    public int shoulderRSortingOffset = 0;
}