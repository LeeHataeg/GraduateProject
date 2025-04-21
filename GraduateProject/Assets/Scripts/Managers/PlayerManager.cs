using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject Player;

    // PlayerPositionController는 반드시 MonoBehaviour로 붙어있는 인스턴스를 사용해야 합니다.
    private PlayerPositionController playerPositionController;

    private void Awake()
    {
        // MonoBehaviour는 new로 인스턴스화하면 안 됨.
        // 만약 PlayerManager와 같은 GameObject에 PlayerPositionController가 부착되어 있지 않으면 추가합니다.
        playerPositionController = GetComponent<PlayerPositionController>();
        if (playerPositionController == null)
        {
            playerPositionController = gameObject.AddComponent<PlayerPositionController>();
        }
    }

    private void Start()
    {
        PlayerInit();
        // RoomManager에서 제공하는 시작 스폰 포인트를 가져온다고 가정하면

    }

    public void PlayerInit()
    {
        // 프리팹 로딩
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Player/Player/Player");


        if (prefab == null)
        {
            Debug.LogError("플레이어 프리팹 로딩 실패");
            return;
        }

        // 프리팹 인스턴스화
        Player = Instantiate(prefab);
        Vector2 startSpawnPoint = GameManager.Instance.RoomManager.StartSpawnPoint;
        if (startSpawnPoint != null)
        {
            // PlayerPositionController의 SetInitialPosition()을 사용해 위치 설정
            playerPositionController.SetPosition(startSpawnPoint);
        }
        else
        {
            Debug.LogWarning("StartSpawnPoint가 존재하지 않습니다.");
        }
    }
}
