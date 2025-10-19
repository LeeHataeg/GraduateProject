using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EchoTape
{
    [Serializable] public struct Frame { public float t; public Vector2 pos; public bool faceRight; public string clip; }
    [Serializable] public struct ActionEvt { public float t; public string kind; public string id; public float value; }

    public List<Frame> frames = new();
    public List<ActionEvt> events = new();
    public HashSet<string> usedItemIds = new();   // 이 플레이에서 실제 사용된 아이템 ID
    public float length;
    public bool wasClear;                         // 클리어/사망 메타
}
