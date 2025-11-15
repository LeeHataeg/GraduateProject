using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : CharacterController
{
    #region MOVE_VARIABLES
    // Move
    private Vector2 moveDir;
    // Look
    private Vector2 lookDir;
    private Vector2 mousePos;
    // Jump, Hit, Inventory state
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
        Unbind();
    }

    private void Bind()
    {
        if (_bound || mainActionMap == null) return;

        if (!mainActionMap.enabled) mainActionMap.Enable();

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
        }

        if (inventoryAction != null) 
            inventoryAction.performed -= OnInventoryPerformed;

        if (hitAction != null) 

            hitAction.performed -= OnHitPerformed;

        if (jumpAction != null) 
            jumpAction.performed -= OnJumpPerformed;

        if (teleportAction != null) 
            teleportAction.performed -= OnTeleportPerformed;

        if (dashAction != null) 
            dashAction.performed -= OnDashPerformed;

        // 씬 전환 시 불필요한 업데이트 루프를 막기 위해 비활성화
        if (mainActionMap.enabled) 
            mainActionMap.Disable();

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
        // 월드 좌표 기준으로 계산
        mousePos = vec;

        Camera cam = Camera.main;
        if (cam == null) 
            return;

        // 카메라에서 mouse 포인터 위치를 웣르 좌표로 변환
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z)));
        
        if (!IsUsable()) 
            return;

        // 화면 기준에서 player transform 기준으로 뵨환
        lookDir = (Vector2)world - (Vector2)transform.position;
        CallLookEvent(lookDir);
    }
    #endregion

    private bool IsUsable()
    {
        if (this == null) 
            return false;
        if (!isActiveAndEnabled) 
            return false;

        Transform t;
        try { 
            t = transform; 
        }
        catch { 
            return false; 
        }

        return t != null;
    }
}
