using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : CharacterController
{

    #region MOVE_VARIABLES
    // Move
    Vector2 moveDir;
    //Look
    Vector2 lookDir;
    Vector2 mousePos;
    //Jump
    float isPressed;
    float isHit;
    float isTurnedOnInven;
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
    
    private void Awake()
    {
        plInput = GetComponent<PlayerInput>();
        mainActionMap = plInput.actions.FindActionMap("Player");

        moveAction = mainActionMap.FindAction("Move");
        teleportAction = mainActionMap.FindAction("Teleport");
        crouchAction = mainActionMap.FindAction("Crouch");
        lookAction = mainActionMap.FindAction("Look");
        hitAction = mainActionMap.FindAction("Hit");
        jumpAction = mainActionMap.FindAction("Jump");
        interactAction = mainActionMap.FindAction("Interaction");
        dashAction = mainActionMap.FindAction("Dash");
        inventoryAction = mainActionMap.FindAction("Inventory");

        crouchAction.performed += ctx =>
        {
            jumpAction.Disable();
            CallCrouchEvent(true);
        };
        crouchAction.canceled += ctx =>
        {
            jumpAction.Enable();
            CallCrouchEvent(false);
        };

        moveAction.performed += ctx => OnMove(ctx.ReadValue<Vector2>());
        moveAction.canceled += ctx => OnMove(ctx.ReadValue<Vector2>());

        lookAction.performed += ctx => OnLook(ctx.ReadValue<Vector2>());

        inventoryAction.performed += ctx => CallInventoryEvent(true);
        hitAction.performed += ctx => CallHitEvent(true);
        jumpAction.performed += ctx => CallJumpEvent(true);
        teleportAction.performed += ctx => CallTeleportEvent(true);
        dashAction.performed += ctx => CallDashEvent(true);

        // TODO - Fix
        //interactAction.performed += ctx => CallInteractEvent(true);
    }
    public void OnMove(Vector2 vec)
    {
        moveDir = vec;
        moveDir = moveDir.normalized;
        CallMoveEvent(moveDir);
    }

    public void OnLook(Vector2 vec)
    {

        mousePos = vec;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        lookDir = mousePos - (Vector2)transform.position;

        CallLookEvent(lookDir);
    }

}
