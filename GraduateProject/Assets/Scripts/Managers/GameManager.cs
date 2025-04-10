using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // This script Manages Operation of Game
    // Like Game Cycle( Gamestart,  etc )
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
            Debug.Log("Destroied GameManager");
        }

        DontDestroyOnLoad(gameObject);
    }


}
