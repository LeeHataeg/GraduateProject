using Unity.VisualScripting;
using UnityEngine;

public class Singletone<T> : MonoBehaviour
{
    private volatile static Singletone<T> instance;

    public static Singletone<T> Instance
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