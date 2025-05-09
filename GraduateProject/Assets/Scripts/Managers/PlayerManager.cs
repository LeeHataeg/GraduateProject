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

        GameManager.Instance.RoomManager.OnSetStartPoint += PlayerInit;
    }

    public void PlayerInit(Vector2 pos)
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

        if (pos != Vector2.zero)
        {
            // PlayerPositionController의 SetInitialPosition()을 사용해 위치 설정
            playerPositionController.SetPosition(pos);
            Debug.LogWarning("StartSpawnPoint : " + pos);

        }
        else
        {
            Debug.LogWarning("StartSpawnPoint가 존재하지 않습니다.");
        }
    }
}
