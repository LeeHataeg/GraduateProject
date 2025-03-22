using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    #region APPREANCE
    SpriteRenderer sprite;

    #endregion

    #region PHYSICS
    [SerializeField][Range(10, 1000)] float speed;

    CharacterController control;

    Rigidbody2D rigid;
    Vector2 dir;

    //TODO - Move this Variable into scripts of 'Stat'
    public float jumpForce = 5.0f;
    public bool isGround;
    public float maxSpeed = 15.0f;
    #endregion

    void Awake()
    {
        control = gameObject.GetComponent<CharacterController>();
        rigid = gameObject.GetComponent<Rigidbody2D>();
        sprite = gameObject.GetComponentInChildren<SpriteRenderer>();
    }
    private void Start()
    {
        control.OnMoveEvent += Move;
        control.OnLookEvent += Look;
        control.OnJumpEvent += Jump;
        control.OnGroundEvent += ContactGround;
    }
    void FixedUpdate()
    {
        ApplayMovement();
    }

    private void Update()
    {
        StopMovement();
    }

    void Move(Vector2 direction)
    {
        dir = direction;
    }
    private void Look(Vector2 direction)
    {
        sprite.flipX = (direction.x < 0);
    }
    void Jump(float isPressed)
    {
        if (isPressed == 1.0f)
        {
            if (isGround)
            {
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpForce);
                rigid.AddForce(new Vector2(rigid.linearVelocity.x, jumpForce), ForceMode2D.Impulse);
                isGround = false;
                Debug.Log(isGround);
            }
        }
    }
    void ContactGround(bool _isGround)
    {
        isGround = _isGround;
        Debug.Log(isGround);
    }
    void ApplayMovement()
    {
        // Can't Jump because ignore other physics
        //rigid.linearVelocity = new Vector2(dir.x * speed, rigid.linearVelocity.y);

        // Fuck - It Don't Work
        rigid.AddForce(dir * speed, ForceMode2D.Impulse);
        if (rigid.linearVelocity.x > maxSpeed)
            rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocity.y);
        if (rigid.linearVelocity.x < maxSpeed * (-1))
            rigid.linearVelocity = new Vector2(maxSpeed * (-1), rigid.linearVelocity.y);
        Debug.Log(rigid.linearVelocity);
    }
    private void StopMovement()
    {
        if(dir.x == 0)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.normalized.x * 0.5f, rigid.linearVelocity.y);
        }
    }
}
