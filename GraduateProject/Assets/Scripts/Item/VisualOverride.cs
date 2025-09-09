using System;
using UnityEngine;

[Serializable]
public class VisualOverride
{
    [Tooltip("어떤 신체 파트를 바꿀지")]
    public Define.BodyPart part;

    [Tooltip("비워두면 icon을 사용 (useIconIfEmpty가 true일 때)")]
    public Sprite sprite;

    [Tooltip("sprite가 비었으면 ItemData.icon을 쓸지?")]
    public bool useIconIfEmpty = true;

    [Header("Per-Part Tweaks")]
    public Vector2 offset;                   // 파트 기준 오프셋
    public Vector2 scale = Vector2.one;      // 배율
    public int sortingOrderOffset = 0;       // 정렬 보정
    public bool hideRenderer = false;        // 파트 숨기기(예: 긴 머리를 헬멧 아래서 숨기기)

    [Header("Mask (optional)")]
    public bool changeMaskInteraction = false;
    public SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None;

    [Tooltip("이 파트에 연결된 SpriteMask(있다면) 켜기/끄기")]
    public bool enablePartSpriteMask = false;
}