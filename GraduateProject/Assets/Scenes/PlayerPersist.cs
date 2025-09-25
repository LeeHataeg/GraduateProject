using UnityEngine;

public class PlayerPersist : MonoBehaviour
{
    static bool s_exists;
    void Awake()
    {
        if (s_exists) { Destroy(gameObject); return; }
        s_exists = true;
        DontDestroyOnLoad(gameObject);
    }
}
