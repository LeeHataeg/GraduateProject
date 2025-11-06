using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Boss/Definition")]
public class BossDefinitionSO : ScriptableObject
{
    public AnimMapSO animMap;

    [Header("Phases (Phase2는 비워둘 수 있음)")]
    public BossPhaseSO phase1;
    public BossPhaseSO phase2;

    [Header("Single-Phase Option")]
    public bool singlePhase = false;

    public enum IntroMode { FallFromSky, PlayEntryOnce }

    [Header("Intro/Idle/Walking Keys")]
    public IntroMode introMode = IntroMode.FallFromSky;
    public AnimKey entryKey = AnimKey.Entry;   // introMode=PlayEntryOnce 일 때 사용
    public AnimKey idleKey = AnimKey.Idle;     // Idle 판정에 사용할 키(상태명)
    public AnimKey walkingBoolKey = AnimKey.Walking;

    [Header("Death/Fade")]
    public AnimKey deathKey = AnimKey.Death;
    public float fadeDelay = 1.2f;
}
