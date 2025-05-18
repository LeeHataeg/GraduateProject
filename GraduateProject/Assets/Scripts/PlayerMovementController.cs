using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    PlayerActionController control;

    Rigidbody2D rb;

    private float jumpForce = 200f;
    private Vector2 upForce;
    private bool isJump = false;

    [SerializeField] private Animator animator;

    private void Awake()
    {
        control = GetComponent<PlayerActionController>();
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();

        upForce = new Vector2(0, jumpForce);
    }

    private void Start()
    {
        control.onJumpEvent += Jump;
    }

    public void Jump(bool isPressed)
    {
        if (isPressed)
        {
            animator.SetBool("isJump", isPressed);
            rb.AddForce(upForce);
            Debug.Log("Jump!");
        }
    }
}
