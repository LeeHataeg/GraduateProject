using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBasicStat", menuName = "Scriptable Objects/PlayerBasicStat")]
public class PlayerBasicStat : ScriptableObject
{
    // TODO - Need to level System
    [field: Header("Level")]
    public int level;
    public int curExp;
    public int nextExp;     // Exp for Next level

    [field: Header("Move")]
    public int speed;       // Moving Speed
    public int jumpForce;   // Distance of Jump
    public int jumpCount;   // Maximum Jump count
}