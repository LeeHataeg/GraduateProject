using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EchoTape
{
    [Serializable] public struct Frame { public float t; public Vector2 pos; public bool faceRight; public string clip; }
    [Serializable] public struct ActionEvt { public float t; public string kind; public string id; public float value; }


    [Serializable]
    public struct AnimParamEvt
    {
        public float t;
        public string type;
        public string name;
        public int value;
    }

    public List<Frame> frames = new();
    public List<ActionEvt> events = new();

    // NEW: Animator 파라미터 이벤트 타임라인
    public List<AnimParamEvt> animParams = new();

    // 사용 아이템 기록(기존)
    public HashSet<string> usedItemIds = new();
    public float length;
    public bool wasClear;                         // 클리어/사망 메타

    // 사망 당시 장비/외형
    [Serializable]
    public struct EquipEntry
    {
        public string slot;     // Define.EquipmentSlot.ToString()
        public string itemId;   // ScriptableObject.name 또는 게임 내 ID
    }

    [Serializable]
    public struct VisualPart
    {
        public string path;
        public string sprite;
        public Vector2 localPosOffset;
        public Vector2 localScaleMul;
        public int sortingOffset;
        public bool enabled;
        public bool changeMaskInteraction;
        public int maskInteraction;
        public bool enablePartSpriteMask;
    }

    public List<EquipEntry> equipped = new();
    public List<VisualPart> visualParts = new();
}
