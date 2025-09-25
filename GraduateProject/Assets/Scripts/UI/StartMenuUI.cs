using UnityEngine;

public class StartMenuUI : MonoBehaviour
{
    // 버튼 OnClick에 연결
    public void OnClickStart()
    {
        SceneLoader.LoadInGame();
    }

    // 선택: 종료 버튼이 있으면 연결
    public void OnClickQuit()
    {
        Application.Quit();
    }
}
