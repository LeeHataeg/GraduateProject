using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private DeathPopupUI Popup => GameManager.Instance?.UIManager?.DeathPopup;

    // PlayerController/PlayerHitReactor의 TODO와 이름 맞춤
    public void ShowGameOver() => TriggerGameOver();

    public void TriggerGameOver()
    {
        var p = Popup;
        if (!p)
        {
            Debug.LogWarning("[GameOverManager] DeathPopupUI not found in this scene.");
            return;
        }
        p.Show();
    }
}
