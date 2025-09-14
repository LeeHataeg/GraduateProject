using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;

public class PlayerMovement : MonoBehaviour
{
    #region APPEARANCE
    private Vector3 playerScale;   // flip용
    private bool alreadyFlip = true;
    #endregion

    #region PHYSICS
    [SerializeField, Range(10, 1000)] private float speed;
    [SerializeField] private float maxSpeed = 15f;

    private CharacterController control;
    private Rigidbody2D rigid;
    private Vector2 dir;

    // Jump 목표 Vy(StatController에는 Get()이 없고, CombatStatSheet에도 JumpForce가 없으므로 기본값 사용)
    [SerializeField] private float defaultJumpVy = 10f;

    private bool isGround = true;
    private bool isPlatform = false;
    #endregion

    private StatController statHolder;
    private Portal currentPortal;

    public float JumpForce => defaultJumpVy;  // 하위 호환용 프로퍼티
    public float Mass => rigid.mass;

    private bool isCrouch;

    private PlayerPlatformDropController plDrop;
    private CompositeCollider2D comCol;

    private Animator animator;

    [Header("UI References")]
    private GameObject inventoryPanel; // 미사용(유지만)
    private bool alreadyTurnedOnInven; // 미사용(유지만)

    private void Awake()
    {
        animator = GetComponent<Animator>();
        control = GetComponent<CharacterController>();
        rigid = GetComponent<Rigidbody2D>();
        plDrop = GetComponent<PlayerPlatformDropController>();
        statHolder = GetComponent<StatController>();
    }

    private void Start()
    {
        playerScale = transform.localScale;

        control.OnMoveEvent += Move;
        control.OnLookEvent += Look;
        control.OnJumpEvent += Jump;
        control.OnDashEvent += Dash;
        control.OnInteractEvent += Interact;
        control.OnTeleportEvent += Teleport;
        control.OnCrouchEvent += Crouch;
        // control.OnHitEvent     += Hit;   // 공격은 PlayerAttackController가 처리하므로 제거
        control.OnInventoryEvent += Inventory;

        // 이동속도는 StatController → CombatStatSheet 에서 직접 읽음
        if (statHolder != null && statHolder.Stats != null)
            speed = statHolder.Stats.MoveSpeed;
    }

    private void Inventory(bool isTurnedOnInven)
    {
        // 키를 누를 때(OnPerformed)만 동작하도록 가정
        if (!isTurnedOnInven) return;
        GameManager.Instance.UIManager.TurnOnorOffInven();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
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
        else if (coll.gameObject.CompareTag("Platform"))
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
            comCol = null;
        }
    }

    private void Move(Vector2 direction)
    {
        dir = direction;
        animator.SetBool("1_Move", dir != Vector2.zero);
    }

    // 마우스 방향에 따른 Flip (lookDir.x 기준)
    private void Look(Vector2 direction)
    {
        if (direction.x < 0)    // Don't-flip
        {
            if (!alreadyFlip)
            {
                playerScale.x *= -1;
                transform.localScale = playerScale;
                alreadyFlip = true;
            }
        }
        else                    // Do-flip
        {
            if (alreadyFlip)
            {
                playerScale.x *= -1;
                transform.localScale = playerScale;
                alreadyFlip = false;
            }
        }
    }

    private void Jump(bool isPressed)
    {
        if (!isPressed) return;
        if (isCrouch) return;

        if (isGround || isPlatform)
        {
            // 목표 상승 속도(CombatStatSheet에 JumpForce가 없으므로 기본값 사용)
            float targetVy = defaultJumpVy;

            // 현재 Vy를 고려하여 필요한 만큼만 Impulse 적용(일관된 점프 높이)
            float curVy = rigid.linearVelocity.y;
            float impulse = (targetVy - curVy) * rigid.mass;
            if (impulse < 0f) impulse = 0f;

            rigid.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);
        }
    }

    public void Crouch(bool isPressing)
    {
        isCrouch = isPressing;

        if (isPlatform && isCrouch)
        {
            plDrop.DropThrough(comCol);
            isCrouch = true;
        }
    }

    public void ResetPlatformFlags()
    {
        isPlatform = false;
    }

    // NPC나 기타 오브젝트와의 상호작용
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

        // 도착 방 입장 처리
        dest.OnPlayerEnter();

        // 연속 텔레포트 방지
        currentPortal = null;

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

    private void ApplyMovement()
    {
        rigid.AddForce(dir * speed, ForceMode2D.Impulse);
        if (rigid.linearVelocity.x > maxSpeed)
            rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocity.y);
        if (rigid.linearVelocity.x < -maxSpeed)
            rigid.linearVelocity = new Vector2(-maxSpeed, rigid.linearVelocity.y);
    }

    private void StopMovement()
    {
        if (dir.x == 0)
        {
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
        }
    }
}
