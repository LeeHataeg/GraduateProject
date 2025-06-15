using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    #region APPREANCE
    // flip용 벡터값
    private Vector3 playerScale;
    private bool alreadyFlip = true;
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

    private bool isCrouch;

    private PlayerPlatformDropController plDrop;
    private CompositeCollider2D comCol;

    Animator animator;

    //temp
    private float curHp = 7f;
    private float atk = 2.4f;
    private Vector2 mouse;

    [Header("UI References")]
    private GameObject inventoryPanel;
    // 기존에 있던…
    private bool alreadyTurnedOnInven;


    private void Awake()
    {
        jumpVec = new Vector2(0, jumpForce);
        animator = GetComponent<Animator>();
        control = gameObject.GetComponent<CharacterController>();
        rigid = gameObject.GetComponent<Rigidbody2D>();
        plDrop = gameObject.GetComponent<PlayerPlatformDropController>();
    }

    private void Start()
    {
        playerScale = gameObject.transform.localScale;

        control.OnMoveEvent += Move;
        control.OnLookEvent += Look;
        control.OnJumpEvent += Jump;
        control.OnDashEvent += Dash;
        control.OnInteractEvent += Interact;
        control.OnTeleportEvent += Teleport;
        control.OnCrouchEvent += Crouch;
        control.OnHitEvent += Hit;
        control.OnInventoryEvent += Inventory;
    }

    private void Inventory(bool isTurnedOnInven)
    {
        // 키를 누를 때(OnPerformed)만 동작하도록 가정
        if (!isTurnedOnInven)
            return;

        GameManager.Instance.UIManager.TurnOnorOffInven();
    }

    private void FixedUpdate()
    {
        ApplayMovement();
    }

    private void Update()
    {
        StopMovement();

        if (curHp <= 0f)
        {
            animator.SetBool("isDeath", true);
        }

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
        if (dir != Vector2.zero)
        {
            animator.SetBool("1_Move", true);
        }
        else
        {
            animator.SetBool("1_Move", false);
        }
    }

    // 마우스 위치에 따른 Flip
    private void Look(Vector2 direction)
    {
        if (direction.x < 0)    // Don't-flip
        {
            if (!alreadyFlip)    // 이미 뒤집혀 있으면 -> 다시 뒤집음
            {
                playerScale.x *= -1;
                gameObject.transform.localScale = playerScale;
                alreadyFlip = true;
            }

        }
        else                    // Do-flip
        {
            if (alreadyFlip)    // 이미 뒤집혀 있으면 -> 다시 뒤집음
            {
                playerScale.x *= -1;
                gameObject.transform.localScale = playerScale;
                alreadyFlip = false;
            }
        }

        mouse = direction;
    }
    private void Jump(bool isPressed)
    {
        // 호출이 Crouch보다 빠름. true, false 할당 전에 불림.
        if (!isPressed) return; 
        if (isCrouch)
        {
            Debug.Log("하단 점프 - Jump에서...");
            return;
        }
        else
        {
            Debug.Log("하단 점프 안하네요 - Jump에서...");
        }

        if (isGround || isPlatform)
        {
            rigid.AddForce(jumpVec, ForceMode2D.Impulse);
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

    public void Hit(bool isHit)
    {
        if (!isHit) return;

        // 1) 프리팹 로드
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Melee_Attck_Effect");
        if (prefab == null)
        {
            Debug.LogError("프리팹을 못 찾음!");
            return;
        }

        // 2) 마우스 방향(lookDir)이 이미 'mouse'에 들어있다고 가정
        Vector2 dir = mouse.normalized;            // 방향만 추출
        Vector3 spawnPos = transform.position      // 플레이어 위치
                         + new Vector3(dir.x, dir.y, 0) * 1f;  // 1유닛 만큼 이동

        Quaternion rot = Quaternion.AngleAxis(
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg,
            Vector3.forward
        );

        GameObject instance = Instantiate(
            prefab,
            spawnPos,
            rot
        );
        // 5) 데미지 세팅
        var effectCtrl = instance.GetComponent<EffectController>();
        effectCtrl?.SetDmg(atk);

        // 7) 애니메이션
        animator.SetTrigger("2_Attack");
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
        Debug.Log("스폰 포인트 : " + targetPos);
        Debug.Log("내 위치 : " + transform.position);

        // 방 진입 시 몬스터 스폰
        dest.OnPlayerEnter();

        Debug.Log("Teleport!");
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
        if (dir.x == 0)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.normalized.x * 0.5f, rigid.linearVelocity.y);
        }
    }
}
