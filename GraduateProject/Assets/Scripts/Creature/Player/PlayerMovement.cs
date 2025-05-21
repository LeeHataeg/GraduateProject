using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    #region APPREANCE
    #endregion

    #region PHYSICS
    [SerializeField][Range(10, 1000)] float speed;

    CharacterController control;

    Rigidbody2D rigid;
    Vector2 dir;

    //TODO - Move this Variable into scripts of 'Stat'
    // TODO - Hide this var to protection
    private float jumpForce = 10.0f;
    private Vector2 jumpVec;
    private bool isGround = true;
    private bool isPlatform = false;
    private float maxSpeed = 15.0f;
    #endregion

    private Portal currentPortal;

    public float JumpForce => jumpForce;
    public float Mass => rigid.mass;

    private bool isCrouch = false;

    private PlayerPlatformDropController plDrop;
    private CompositeCollider2D comCol;
    private PlayerAttackController playerAttackController;

    private void Awake()
    {
        jumpVec = new Vector2(0, jumpForce);
        control = gameObject.GetComponent<CharacterController>();
        rigid = gameObject.GetComponent<Rigidbody2D>();
        plDrop = gameObject.GetComponent<PlayerPlatformDropController>();
        playerAttackController = gameObject.GetComponentInChildren<PlayerAttackController>();
        
    }
    private void Start()
    {
        control.OnMoveEvent += Move;
        control.OnLookEvent += Look;
        control.OnJumpEvent += Jump;
        control.OnDashEvent += Dash;
        control.OnInteractEvent += Interact;
        control.OnTeleportEvent += Teleport;
        control.OnCrouchEvent += Crunch;
        control.OnHitEvent += Hit;
    }
    private void FixedUpdate()
    {
        ApplayMovement();
    }

    private void Update()
    {
        StopMovement();
    }

    private void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }
        else if(coll.gameObject.CompareTag("Platform"))
        {
            isPlatform = true;
            comCol = coll.gameObject.GetComponent<CompositeCollider2D>();
        }
    }

    private void OnCollisionExit2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Ground"))
        {
            isGround = false;
        }
        else if (coll.gameObject.CompareTag("Platform"))
        {
            isPlatform = false;
            isCrouch = false;
            comCol = null;
        }
    }

    private void Move(Vector2 direction)
    {
        dir = direction;
    }
    private void Look(Vector2 direction)
    {
        //
    }
    private void Jump(bool isPressed)
    {
        if (!isPressed) return;

        if (isGround || isPlatform)
        {
            Debug.Log("Jump 로직 진입");
            if (!isCrouch)
            {
                rigid.AddForce(jumpVec, ForceMode2D.Impulse);
            }
            else
            {
                plDrop.DropThrough(comCol);
                isPlatform = false;
                isCrouch = false;
                comCol = null;
            }
        }
    }

    public void Hit(bool isHit)
    {
        if (isHit)
        {
            playerAttackController.Hit();
        }
    }

    public void ResetPlatformFlags()
    {
        isPlatform = false;
        isCrouch = false;
    }
    public void Crunch(bool isPressed)
    {
        isCrouch = isPressed;
        

    }

    private void Interact(bool isInteracted)
    {
        Debug.Log("Interact!"); 
    }

    private void Dash(bool isDashed)
    {
        Debug.Log("Dash!");
    }

    public void Teleport(bool isTeleported)
    {
        if (!isTeleported || currentPortal == null)
            return;

        Room dest = currentPortal.GetDestinationRoom();
        if (dest == null)
        {
            Debug.LogWarning("Teleport 실패: 연결된 방이 없습니다.");
            return;
        }

        Vector2 targetPos = dest.GetSpawnPosition();
        transform.position = targetPos;

        // 방 진입 시 몬스터 스폰
        dest.OnPlayerEnter();

        Debug.Log("OnTeleport!");
    }


    public void SetCurrentPortal(Portal portal)
    {
        currentPortal = portal;
    }

    public void ClearCurrentPortal(Portal portal)
    {
        if (currentPortal == portal)
            currentPortal = null;
    }

    void ApplayMovement()
    {
        rigid.AddForce(dir * speed, ForceMode2D.Impulse);
        if (rigid.linearVelocity.x > maxSpeed)
            rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocity.y);
        if (rigid.linearVelocity.x < maxSpeed * (-1))
            rigid.linearVelocity = new Vector2(maxSpeed * (-1), rigid.linearVelocity.y);
    }
    private void StopMovement()
    {
        if(dir.x == 0)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.normalized.x * 0.5f, rigid.linearVelocity.y);
        }
    }
}
