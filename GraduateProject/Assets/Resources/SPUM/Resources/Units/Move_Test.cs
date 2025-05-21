using UnityEngine;

public class Move_Test : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 moveVector = new Vector3(moveX, moveY, 0f);

        transform.Translate(moveVector.normalized * Time.deltaTime * 5f);

        if (moveX != 0 || moveY != 0)
        {
            animator.SetBool("1_Move", true);
        }
        else
        {
            animator.SetBool("1_Move", false);

        }

        if (Input.GetKeyDown(KeyCode.Z))
            animator.SetTrigger("2_Attack");
    }
}
