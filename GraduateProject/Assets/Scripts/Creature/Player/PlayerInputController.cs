using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 원본 구조 유지 + 개선:
/// 1) 입력 콜백 구독을 Awake → OnEnable, 해제를 OnDisable/OnDestroy로 이동(수명 주기 대칭).
/// 2) 람다 구독 제거 → 전용 메서드로 구독/해제 가능(안전한 Unbind 보장).
/// 3) 중복 바인딩 방지 플래그, 액션맵 Enable/Disable 관리.
/// 4) Camera.main/transform 접근 널 가드(IsUsable).
/// 5) 보스 필드 언로드/씬 전환 시 남는 콜백으로 인한 MissingReferenceException 방지.
/// </summary>
public class PlayerInputController : CharacterController
{
    #region MOVE_VARIABLES
    // Move
    private Vector2 moveDir;
    // Look
    private Vector2 lookDir;
    private Vector2 mousePos;
    // Jump / Hit / Inventory state (필요시 사용)
    private float isPressed;
    private float isHit;
    private float isTurnedOnInven;
    #endregion

    #region InputSys_Behavior
    private PlayerInput plInput;
    private InputActionMap mainActionMap;

    private InputAction moveAction;
    private InputAction teleportAction;
    private InputAction crouchAction;
    private InputAction lookAction;
    private InputAction hitAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private InputAction dashAction;
    private InputAction inventoryAction;
    #endregion

    private bool _bound;

    private void Awake()
    {
        plInput = GetComponent<PlayerInput>();

        if (plInput != null)
        {
            // 우선 Player라는 이름의 맵을 찾고, 없으면 currentActionMap 사용
            mainActionMap = plInput.actions?.FindActionMap("Player", throwIfNotFound: false)
                           ?? plInput.currentActionMap;
        }

        if (mainActionMap == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[{nameof(PlayerInputController)}] ActionMap을 찾지 못했습니다. PlayerInput 설정을 확인하세요.", this);
#endif
            return;
        }

        // 액션 참조만 잡아두고, 구독은 OnEnable에서 한다.
        moveAction = mainActionMap.FindAction("Move", throwIfNotFound: false);
        teleportAction = mainActionMap.FindAction("Teleport", throwIfNotFound: false);
        crouchAction = mainActionMap.FindAction("Crouch", throwIfNotFound: false);
        lookAction = mainActionMap.FindAction("Look", throwIfNotFound: false);
        hitAction = mainActionMap.FindAction("Hit", throwIfNotFound: false);
        jumpAction = mainActionMap.FindAction("Jump", throwIfNotFound: false);
        interactAction = mainActionMap.FindAction("Interaction", throwIfNotFound: false);
        dashAction = mainActionMap.FindAction("Dash", throwIfNotFound: false);
        inventoryAction = mainActionMap.FindAction("Inventory", throwIfNotFound: false);
    }

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnDestroy()
    {
        // 파괴 경로에서도 혹시 남아있을 구독 제거(안전망)
        Unbind();
    }

    private void Bind()
    {
        if (_bound || mainActionMap == null) return;

        if (!mainActionMap.enabled) mainActionMap.Enable();

        // === 콜백 구독(람다 → 메서드로 변경하여 언바인드 가능) ===
        if (crouchAction != null)
        {
            crouchAction.performed += OnCrouchPerformed;
            crouchAction.canceled += OnCrouchCanceled;
        }

        if (moveAction != null)
        {
            moveAction.performed += OnMovePerformedOrCanceled;
            moveAction.canceled += OnMovePerformedOrCanceled;
        }

        if (lookAction != null)
        {
            lookAction.performed += OnLookPerformed;
            // 필요시 canceled에서도 0 벡터 전달 등 처리 가능
            // lookAction.canceled  += OnLookCanceled;
        }

        if (inventoryAction != null) inventoryAction.performed += OnInventoryPerformed;
        if (hitAction != null) hitAction.performed += OnHitPerformed;
        if (jumpAction != null) jumpAction.performed += OnJumpPerformed;
        if (teleportAction != null) teleportAction.performed += OnTeleportPerformed;
        if (dashAction != null) dashAction.performed += OnDashPerformed;

        // TODO - Fix
        // if (interactAction != null) interactAction.performed += OnInteractPerformed;

        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;

        if (crouchAction != null)
        {
            crouchAction.performed -= OnCrouchPerformed;
            crouchAction.canceled -= OnCrouchCanceled;
        }

        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformedOrCanceled;
            moveAction.canceled -= OnMovePerformedOrCanceled;
        }

        if (lookAction != null)
        {
            lookAction.performed -= OnLookPerformed;
            // lookAction.canceled  -= OnLookCanceled;
        }

        if (inventoryAction != null) inventoryAction.performed -= OnInventoryPerformed;
        if (hitAction != null) hitAction.performed -= OnHitPerformed;
        if (jumpAction != null) jumpAction.performed -= OnJumpPerformed;
        if (teleportAction != null) teleportAction.performed -= OnTeleportPerformed;
        if (dashAction != null) dashAction.performed -= OnDashPerformed;

        // if (interactAction != null) interactAction.performed -= OnInteractPerformed;

        // 씬 전환 시 불필요한 업데이트 루프를 막기 위해 비활성화
        if (mainActionMap.enabled) mainActionMap.Disable();

        _bound = false;
    }

    #region Input Callbacks (performed/canceled 핸들러)
    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        if (jumpAction != null && jumpAction.enabled) jumpAction.Disable();
        CallCrouchEvent(true);
    }

    private void OnCrouchCanceled(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        if (jumpAction != null && !jumpAction.enabled) jumpAction.Enable();
        CallCrouchEvent(false);
    }

    private void OnMovePerformedOrCanceled(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        OnMove(ctx.ReadValue<Vector2>());
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        OnLook(ctx.ReadValue<Vector2>());
    }

    private void OnInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        CallInventoryEvent(true);
    }

    private void OnHitPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        CallHitEvent(true);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        CallJumpEvent(true);
    }

    private void OnTeleportPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        CallTeleportEvent(true);
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUsable()) return;
        CallDashEvent(true);
    }

    // private void OnInteractPerformed(InputAction.CallbackContext ctx)
    // {
    //     if (!IsUsable()) return;
    //     CallInteractEvent(true);
    // }
    #endregion

    #region Original Public Methods (원본 시그니처 유지)
    public void OnMove(Vector2 vec)
    {
        moveDir = vec.normalized;
        CallMoveEvent(moveDir);
    }

    public void OnLook(Vector2 vec)
    {
        // 화면 좌표 → 월드 좌표
        mousePos = vec;

        Camera cam = Camera.main;
        if (cam == null) return; // 씬 전환 타이밍에 메인 카메라가 아직 없을 수 있음

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z)));
        // transform 접근 시점 가드
        if (!IsUsable()) return;

        lookDir = (Vector2)world - (Vector2)transform.position;
        CallLookEvent(lookDir);
    }
    #endregion

    /// <summary>
    /// 파괴/비활성/트랜스폼 접근 불가 등 예외 타이밍 가드
    /// </summary>
    private bool IsUsable()
    {
        if (this == null) return false;
        if (!isActiveAndEnabled) return false;

        // Unity 특성상 파괴 직후 프레임에 transform 접근 시 예외가 날 수 있으므로 한 번 잡아준다.
        Transform t;
        try { t = transform; }
        catch { return false; }

        return t != null;
    }
}
