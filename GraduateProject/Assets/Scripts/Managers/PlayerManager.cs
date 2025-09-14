using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject Player;

    public event Action<EquipmentManager> OnEquipmentReady;         // ★ 핵심

    private PlayerPositionController playerPositionController;

    private void Awake()
    {
        playerPositionController = GetComponent<PlayerPositionController>();
        if (playerPositionController == null)
            playerPositionController = gameObject.AddComponent<PlayerPositionController>();

        GameManager.Instance.RoomManager.OnSetStartPoint += PlayerInit;
    }

    public void PlayerInit(Vector2 pos)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Player/Player/Player");
        Player = Instantiate(prefab);

        if (pos != Vector2.zero)
            playerPositionController.SetPosition(pos);

        // ★ 반드시 Unit Root 기준으로 가져오기
        var unitRoot = Player.transform.Find("Unit Root");
        if (unitRoot == null)
        {
            // 혹시 이름이 다르면, Unit Root에 항상 있는 컴포넌트로 대체 탐색
            unitRoot = Player.GetComponentInChildren<PlayerMovement>(true)?.transform;
        }

        if (unitRoot == null)
        {
            Debug.LogError("[PlayerInit] Unit Root not found under Player(Clone).");
            return;
        }

        // ★ ‘추가(AddComponent)’ 하지 말고, ‘자식에서 찾기’만 한다
        var eq = unitRoot.GetComponent<EquipmentManager>();
        var stat = unitRoot.GetComponent<StatController>();

        if (eq == null || stat == null)
        {
            Debug.LogError($"[PlayerInit] Missing components on Unit Root. eq={eq}, stat={stat}");
            return;
        }

        // (선택) 다른 UI/시스템에 eq 전달
        OnEquipmentReady?.Invoke(eq);
    }
}
