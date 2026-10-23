using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player1Controller : MonoBehaviour
{

    public float runSpeed = 6f;
    public float jumpForce = 8f;

    [SerializeField] private Animator animator;

    Rigidbody2D rb;
    SpriteRenderer sprite;
    InputAction moveAction;
    InputAction jumpAction;
    bool isGrounded;

    //Triggers once on startup
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }
    //Triggers every frame (60fps)
    void Update()
    {
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }
        if (isGrounded == false)
        {
            animator.SetBool("Jump", true);
        }
        else
        {
            animator.SetBool("Jump", false);
        }
        animator.SetFloat("yVelocity", rb.linearVelocityY);
    }
    void FixedUpdate()
    {
        float x = moveAction.ReadValue<Vector2>().x;
        rb.linearVelocity = new Vector2(x * runSpeed, rb.linearVelocity.y);

        //if x doesn't equal 0, flip the sprite's x axis to a variable less than 0
        if (x != 0)
        {
            sprite.flipX = x < 0;
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
	}
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground")) isGrounded = false;
    }
}
