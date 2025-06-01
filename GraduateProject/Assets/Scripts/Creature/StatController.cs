using UnityEngine;

public class StatController : MonoBehaviour
{
    [SerializeField] private CombatStatSheet stats;  // 인스펙터로 할당

    public CombatStatSheet Stats
    {
        get
        {
            if (stats == null)
            {
                Debug.LogError($"[{nameof(StatController)}] {gameObject.name} 에 CombatStatSheet가 할당되지 않았습니다.");
            }
            return stats;
        }
    }

    private void Reset()
    {
        // 만약 인스펙터에 할당이 누락되었으면 자동으로 같은 GameObject의 Asset을 대입 시도
        if (stats == null)
        {
            stats = GetComponent<CombatStatSheet>();
        }
    }

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"[{nameof(StatController)}] Awake 시점에 stats가 null입니다. 인스펙터에서 할당해주세요.");
        }
    }
}
