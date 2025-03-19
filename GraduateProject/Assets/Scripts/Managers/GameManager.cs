using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private volatile static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            //TODO - Add details
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }
}
