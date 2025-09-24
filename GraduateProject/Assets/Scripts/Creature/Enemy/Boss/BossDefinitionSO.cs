using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Boss/Definition")]
public class BossDefinitionSO : ScriptableObject
{
    public AnimMapSO animMap;
    public BossPhaseSO phase1;
    public BossPhaseSO phase2;

    [Header("Death/Fade")]
    public AnimKey deathKey = AnimKey.Death;
    public float fadeDelay = 1.2f;
}
