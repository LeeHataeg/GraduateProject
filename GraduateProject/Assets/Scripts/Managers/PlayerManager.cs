using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject Player;

    public event Action<GameObject> OnPlayerSpawned;                // 선택: 필요시 씀
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
        if (prefab == null) { Debug.LogError("플레이어 프리팹 로딩 실패"); return; }

        Player = Instantiate(prefab);

        // 위치 세팅
        if (pos != Vector2.zero) playerPositionController.SetPosition(pos);

        // ★ 장비 매니저 확보(없으면 붙이고, 인벤토리 물려줌)
        var eq = Player.GetComponent<EquipmentManager>();
        if (eq == null) eq = Player.AddComponent<EquipmentManager>();
        if (!eq.inventory) eq.inventory = FindFirstObjectByType<InventorySystem>();

        // 알림
        OnPlayerSpawned?.Invoke(Player);
        OnEquipmentReady?.Invoke(eq);
    }
}
