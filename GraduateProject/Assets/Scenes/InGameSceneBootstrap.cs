// InGameSceneBootstrap.cs
using UnityEngine;

public class InGameSceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // 이 씬의 UIManager를 미리 강제 캐시해서 초기화 순서 걱정 제거
        var ui = FindFirstObjectByType<UIManager>();
        GameManager.Instance?.RegisterUIManager(ui);
    }
}
