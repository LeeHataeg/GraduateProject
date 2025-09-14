using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;

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
    private float jumpForce = 0.2f;
    private bool isGround = true;
    private bool isPlatform = false;
    private float maxSpeed = 15.0f;
    #endregion

    private PlayerStatController stat;

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
        animator = GetComponent<Animator>();
        control = GetComponent<CharacterController>();
        rigid = GetComponent<Rigidbody2D>();
        plDrop = GetComponent<PlayerPlatformDropController>();
        stat = GetComponent<PlayerStatController>();
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
        // ★중복 공격 방지: 아래 한 줄 삭제
        // control.OnHitEvent += Hit;

        control.OnInventoryEvent += Inventory;

        if (stat != null)
        {
            // 이동속도만 스탯 반영
            speed = stat.Get(Define.StatType.MoveSpeed);

            stat.OnStatsChanged += () =>
            {
                speed = stat.Get(Define.StatType.MoveSpeed);
                // 점프는 여기서 미리 계산하지 않음(중복 방지)
            };
        }
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
        if (!isPressed) return;
        if (isCrouch) return;

        if (isGround || isPlatform)
        {
            // 목표 상승 속도(Stat의 JumpForce는 "원하는 Vy")
            float targetVy = stat ? stat.Get(Define.StatType.JumpForce) : 10f;

            // 현재 Vy를 고려하여 딱 필요한 만큼만 Impulse 적용(일관된 점프 높이)
            float curVy = rigid.linearVelocity.y;
            float impulse = (targetVy - curVy) * rigid.mass;
            if (impulse < 0f) impulse = 0f; // 이미 위로 날아가는 중이면 0으로

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

    // PlayerMovement.cs (Teleport 메서드 내부)
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

        // ▼ 추가: 도착 방 입장 처리
        dest.OnPlayerEnter();

        // ▼ 선택: 연속 텔레포트 방지
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
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
        }
    }
}
