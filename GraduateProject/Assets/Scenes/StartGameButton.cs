using UnityEngine;

public class StartGameButton : MonoBehaviour
{
    // UI Button OnClick에 이 메서드를 연결
    public void OnClickStartGame()
    {
        SceneLoader.LoadInGame();
    }
}
