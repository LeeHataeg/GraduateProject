using UnityEngine;

[DisallowMultipleComponent]
public class EnemyScore : MonoBehaviour
{
    [Tooltip("이 적 개체의 아키타입 (점수 등 정보 포함)")]
    public EnemyArchetypeSO archetype;

    private HealthController _hp;
    private bool _subscribed;

    private void Awake()
    {
        _hp = GetComponent<HealthController>();
        if (_hp == null)
            _hp = GetComponentInChildren<HealthController>(true);
    }

    private void OnEnable()
    {
        if (_hp != null && !_subscribed)
        {
            _hp.OnDead += HandleDead;
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_hp != null && _subscribed)
        {
            _hp.OnDead -= HandleDead;
            _subscribed = false;
        }
    }

    public void Setup(EnemyArchetypeSO arch)
    {
        archetype = arch;
    }

    private void HandleDead()
    {
        var rm = GameManager.Instance.RankingManager;
        if (rm != null)
            rm.OnEnemyKilled(archetype);
    }
}