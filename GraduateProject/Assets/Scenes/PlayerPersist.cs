using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerPersist : MonoBehaviour
{
    private static bool s_DDOLRegistered;

    private void Awake()
    {
        // 절대 GameObject를 Destroy하지 말 것!
        // 여기서는 루트 GO만 DDOL 등록하고, 이 컴포넌트는 제거해서 간섭 종료.

        var root = transform.root != null ? transform.root.gameObject : gameObject;

        if (!s_DDOLRegistered)
        {
            try
            {
                DontDestroyOnLoad(root);
                s_DDOLRegistered = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[PlayerPersist] Registered root as DontDestroyOnLoad.");
#endif
            }
            catch { /* ignore */ }
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[PlayerPersist] DDOL already registered. Leaving object as-is (no destroy).");
#endif
        }

        // 이 컴포넌트는 더 이상 필요 없음 — 자기 자신만 제거
        Destroy(this);
    }
}
