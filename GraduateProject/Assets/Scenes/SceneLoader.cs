using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string StartSceneName = "StartScene";
    public const string InGameSceneName = "InGameScene";

    public static void LoadStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(StartSceneName);
    }

    public static void LoadInGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(InGameSceneName);
    }

    public static void ReloadCurrent()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
