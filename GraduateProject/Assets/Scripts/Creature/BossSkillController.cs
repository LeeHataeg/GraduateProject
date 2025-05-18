using UnityEngine;

public class BossSkillController : MonoBehaviour, IBoss
{
    public int CurrentPhase { get; private set; }
    public void EnterPhase(int phaseIndex)
    {
        CurrentPhase = phaseIndex;
        // 페이즈별 스킬 로직
    }
}
