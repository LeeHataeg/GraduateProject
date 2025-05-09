using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    #region APPREANCE
    SpriteRenderer sprite;

    #endregion

    #region PHYSICS
    [SerializeField][Range(10, 1000)] float speed;

    CharacController control;

    Rigidbody2D rigid;
    Vector2 dir;

    //TODO - Move this Variable into scripts of 'Stat'
    // TODO - Hide this var to protection
    private float jumpForce = 200.0f;
    private Vector2 jumpVec;
    private bool isGround = true;
    private float maxSpeed = 15.0f;
    #endregion

    private void Awake()
    {
        jumpVec = new Vector2(0, jumpForce);
        control = gameObject.GetComponent<CharacController>();
        rigid = gameObject.GetComponent<Rigidbody2D>();
        sprite = gameObject.GetComponentInChildren<SpriteRenderer>();
    }
    private void Start()
    {
        control.OnMoveEvent += Move;
        control.OnLookEvent += Look;
        control.OnJumpEvent += Jump;
        control.OnDashEvent += Dash;
        control.OnInteractEvent += Interact;
        control.OnTeleportEvent += Teleport;

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
    }

    private void OnCollisionExit2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Ground"))
        {
            isGround = false;
        }
    }

    private void Move(Vector2 direction)
    {
        dir = direction;
    }
    private void Look(Vector2 direction)
    {
        sprite.flipX = (direction.x < 0);
    }
    private void Jump(bool isPressed)
    {
        if (isPressed)
        {
            if (isGround)
            {
                rigid.AddForce(jumpVec, ForceMode2D.Impulse);
            }
        }
    }

    private void Interact(bool isInteracted)
    {
        Debug.Log("Interact!");
    }

    private void Dash(bool isDashed)
    {
        Debug.Log("Dash!");
    }

    private void Teleport(bool isTeleported)
    {
        Debug.Log("OnTeleport!");
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
