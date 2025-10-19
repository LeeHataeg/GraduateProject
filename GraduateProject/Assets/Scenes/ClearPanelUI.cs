using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearPanelUI : MonoBehaviour
{
    [Header("Scene Names")]
    public string startSceneName = "StartScene";
    public string inGameSceneName = "InGameScene";

    [Header("Options")]
    public bool normalizeTimeScale = true;

    public void OnClickRestart()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();                 // ★ 상태 초기화
        SceneManager.LoadScene(inGameSceneName, LoadSceneMode.Single);
    }

    public void OnClickHome()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();                 // ★ 상태 초기화
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    private void TrySoftResetManagers()
    {
        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        // UI 전부 닫기(있으면)
        gm.UIManager?.SendMessage("HideAll", SendMessageOptions.DontRequireReceiver);

        // ★★ 핵심: 다음 회차를 위해 시작점/방 정보 초기화
        gm.RoomManager?.ResetRooms();

        // PlayerManager 내부 상태가 필요하다면 다음 줄도 고려(메서드가 없다면 생략)
        // gm.PlayerManager?.SendMessage("ResetState", SendMessageOptions.DontRequireReceiver);
    }
}
