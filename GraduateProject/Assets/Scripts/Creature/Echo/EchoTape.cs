using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EchoTape
{
    [Serializable] public struct Frame { public float t; public Vector2 pos; public bool faceRight; public string clip; }
    [Serializable] public struct ActionEvt { public float t; public string kind; public string id; public float value; }

    // ─────────────────────────────────────────────
    // 사망 당시 장비/외형 스냅샷
    // ─────────────────────────────────────────────
    [Serializable]
    public struct EquipEntry
    {
        public string slot;     // Define.EquipmentSlot.ToString()
        public string itemId;   // ScriptableObject.name 또는 게임 내 ID
    }

    [Serializable]
    public struct VisualPart
    {
        // ★ Root 하위 상대 경로(예: "Torso/Chest/ChestSprite")
        public string path;

        public string sprite;   // Sprite.name
        public Vector2 localPosOffset; // 기본값 대비 오프셋(없다면 0)
        public Vector2 localScaleMul;  // 기본값 대비 스케일 비율(없다면 1)
        public int sortingOffset;      // 기본 sortingOrder 대비 오프셋
        public bool enabled;

        // 마스크 관련(있으면 반영)
        public bool changeMaskInteraction;
        public int maskInteraction;  // (int)SpriteMaskInteraction
        public bool enablePartSpriteMask;
    }

    public List<Frame> frames = new();
    public List<ActionEvt> events = new();

    // 사용 아이템 기록(기존)
    public HashSet<string> usedItemIds = new();
    public float length;
    public bool wasClear;                         // 클리어/사망 메타

    // 사망 당시 장비/외형
    public List<EquipEntry> equipped = new();
    public List<VisualPart> visualParts = new();
}
