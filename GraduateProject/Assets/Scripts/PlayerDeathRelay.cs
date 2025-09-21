using UnityEngine;

[RequireComponent(typeof(HealthController))]
public class PlayerDeathRelay : MonoBehaviour
{
    private HealthController health;

    private void Awake()
    {
        health = GetComponent<HealthController>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDead += HandleDead;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDead -= HandleDead;
    }

    private void HandleDead()
    {
        GameOverManager.Instance?.TriggerGameOver();
    }
}
