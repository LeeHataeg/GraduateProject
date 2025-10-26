using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    private InventorySlot slotData;
    private int slotIndex = -1;
    private float lastClickTime = -999f;
    private const float DOUBLE_CLICK = 0.25f;

    private InventorySystem inventory;
    private EquipmentManager equipment;
    private InventoryUI owner;

    void Awake()
    {
        owner = GetComponentInParent<InventoryUI>(true);
    }
    public void Bind(InventoryUI ownerUI, InventorySystem inv)
    {
        owner = ownerUI;
        inventory = inv;
    }

    // 인덱스 포함 버전
    public void SetData(InventorySlot slot, int index)
    {
        slotData = slot;
        slotIndex = index;

        // ★ 안전 가드: 혹시라도 아직 바인딩 안 됐으면 부모에서 가져옴
        if (inventory == null && owner != null) inventory = owner.GetInventoryUnsafe();

        if (slotData?.item != null)
        {
            iconImage.sprite = slotData.item.icon;
            iconImage.enabled = true;
            quantityText.text = slotData.quantity > 1 ? slotData.quantity.ToString() : string.Empty;
        }
        else
        {
            SetEmpty();
        }
    }

    public void SetEmpty()
    {
        slotData = null;
        slotIndex = -1;
        iconImage.enabled = false;
        quantityText.text = string.Empty;
    }

    // InventorySlotUI.cs (OnPointerClick 내부만 교체)
    public void OnPointerClick(PointerEventData e)
    {
        // 좌클릭만
        if (e.button != PointerEventData.InputButton.Left) return;
        if (slotData == null || slotData.item == null) return;

        Debug.Log($"[INV] UI inv={owner?.GetInventoryUnsafe()?.GetInstanceID()} slotUI.inv={inventory?.GetInstanceID()} UI-same={(owner?.GetInventoryUnsafe() == inventory)} idx={slotIndex}");

        // 더블클릭 판정
        float now = Time.unscaledTime;
        bool isDouble = (now - lastClickTime) <= DOUBLE_CLICK;
        lastClickTime = now;
        if (!isDouble) return;

        // 장비 아이템만
        if (!(slotData.item is EquipmentItemData eqData))
        {
            Debug.Log($"[INV] dbl-click ignored: not equipment ({slotData.item.name})");
            return;
        }

        // 런타임 플레이어에서 EquipmentManager 찾기
        if (equipment == null)
        {
            var player = GameManager.Instance?.PlayerManager?.Player;
            if (player != null)
                equipment = player.GetComponentInChildren<EquipmentManager>(true);
        }
        if (equipment == null) return;

        // 1) 장착 시도 (인벤토리 수정 X, 교체 결과만 out)
        if (equipment.TryEquip(eqData, out var prevEquipped))
        {
            // === [ADD] Echo Runner: 장착을 '사용'으로 기록 ===
            EchoInventoryBridge.RaiseItemUsed(this.gameObject, eqData.name);

            var playerGO = GameManager.Instance?.PlayerManager?.UnitRoot;
            if (playerGO != null)
            {
                EchoInventoryBridge.RaiseItemUsed(playerGO, eqData.name); // 또는 eqData.itemName 등 식별자
            }

            // 2) 현재 클릭한 인벤 슬롯에서 정확히 1개 제거(★ 먼저 제거)
            //    이 시점에서 빈칸이 1칸 생김 → 교체품 넣어도 인덱스/용량 문제 없음
            if (!inventory.RemoveAt(slotIndex, 1))
            {
                Debug.LogWarning("[INV] RemoveAt failed after equip. Consider rollback logic.");
                return;
            }

            // 3) 교체였다면 이전 장비 1개를 인벤토리에 추가
            if (prevEquipped != null)
            {
                // RemoveAt로 이미 1칸 비웠으므로 실패하지 않음
                inventory.AddItem(prevEquipped, 1);
            }
            // InventorySystem이 이벤트를 쏘므로 UI는 자동 갱신됨
        }
    }
}
