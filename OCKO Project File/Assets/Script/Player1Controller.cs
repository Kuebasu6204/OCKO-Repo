using UnityEngine;
using UnityEngine.InputSystem;

public class Player1Controller : MonoBehaviour
{
    public float runSpeed = 6f;
    public float jumpForce = 6f;


    Rigidbody2D rb;
    InputAction P1Move;
    InputAction P1Jump;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        P1Move = InputSystem.actions.FindAction("Horizontal1");
        P1Jump = InputSystem.actions.FindAction("Jump1");
    }

    void FixedUpdate()
    {
        float x = P1Move.ReadValue<Vector2>().x;
        rb.linearVelocity = new Vector2(x * runSpeed, rb.linearVelocity.y);
    }
}
