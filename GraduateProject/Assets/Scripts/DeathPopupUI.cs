using UnityEngine;
using UnityEngine.UI;

public class DeathPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;      // 팝업 최상위 오브젝트(없으면 자기 자신)
    [SerializeField] private Button restartButton; // Restart
    [SerializeField] private Button homeButton;    // Home

    private void Awake()
    {
        if (!root) root = gameObject;
        root.SetActive(false); // 기본 비활성

        if (restartButton) restartButton.onClick.AddListener(OnClickRestart);
        if (homeButton) homeButton.onClick.AddListener(OnClickHome);
    }

    public void Show()
    {
        root.SetActive(true);
        Time.timeScale = 0f; // 게임 일시정지
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        root.SetActive(false);
    }

    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneLoader.ReloadCurrent();
    }

    private void OnClickHome()
    {
        Time.timeScale = 1f;
        SceneLoader.LoadStart();
    }
}
