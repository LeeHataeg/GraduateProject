using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageTransitionPortal : MonoBehaviour
{
    private void Start()
    {
        this.gameObject.SetActive(true);
    }

    private void Reset()
    {
        if (TryGetComponent<Collider2D>(out var col))
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag("Player")) return;

        // Stage1 → Stage2 전환
        GameManager.Instance?.AdvanceToNextStage();

        // Destroy(gameObject); // 원하면 1회용
    }
}
