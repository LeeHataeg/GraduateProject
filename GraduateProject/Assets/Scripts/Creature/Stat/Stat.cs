using UnityEngine;

public class Stat : ScriptableObject
{
    // 1. level
    // 2.HP, MP, DEF, ATK, DEX, INT, LUK, 
    // 3. SPEED, CRI CHANCE, CRI DMG, JUMP
    [SerializeField] int maxHp;
    [SerializeField] int dmg;
}
