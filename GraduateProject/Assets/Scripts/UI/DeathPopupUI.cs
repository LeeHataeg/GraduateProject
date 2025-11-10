using UnityEngine;
using UnityEngine.UI;

public class DeathPopupUI : MonoBehaviour
{
    [SerializeField] private Button homeBtn;
    [SerializeField] private Button restartBtn;

    private void Awake()
    {
        if (homeBtn) homeBtn.onClick.AddListener(OnClickHome);
        if (restartBtn) restartBtn.onClick.AddListener(OnClickRestart);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private void OnClickHome()
    {
        Hide();
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
    }

    private void OnClickRestart()
    {
        // 팝업 닫고 현재 스테이지를 그대로 재시작(씬 리로드 X)
        Hide();
        GameManager.Instance?.RestartRun();
    }
}
