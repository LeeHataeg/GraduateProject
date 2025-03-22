using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBattleStat", menuName = "Scriptable Objects/PlayerBattleStat")]
public class PlayerBasicBattleStat : Stat
{
    [field: Header("#NormalStat")]
    [SerializeField] int curHp;

    [SerializeField] int maxMp;
    [SerializeField] int genMp;
    [SerializeField] int curMp;

    [field: Header("#DevelopStat")]     //Update By LevelUp
    [SerializeField] int atk;
    [SerializeField] int def;
    [SerializeField] int intel;
    [SerializeField] int luck; // drop per?

    [field: Header("#BattleStat")]
    [SerializeField] int critic;
    [SerializeField] int criticDmg;
}