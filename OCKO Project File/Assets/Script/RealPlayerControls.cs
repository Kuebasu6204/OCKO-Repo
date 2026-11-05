using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RealPlayerControls : MonoBehaviour
{
    public Rigidbody2D rb;
    BoxCollider2D playerCollider;


    public float moveSpeed = 5f;
    float horizontalMovement;

    public float jumpPower = 10f;
    public int maxJumps = 2;
    int jumpsRemaining;

    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isOnPlatform;

    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMuliplier = 2f;

    public float crouchSpeedMultiplier = 0.5f;
    public float doubleTapWindow = 0.25f;  
    private float lastDownPressTime = -10f;
    private bool isCrouching;

    private Vector2 originalSize;
    private Vector2 originalOffset;
    public Transform spriteTransform;      
    public float crouchScaleY = 0.5f;
    public float scaleLerpSpeed = 15f;      // smooth transition; ~10–20 feels good

    private float targetScaleY = 1f;

    [Header("Attack")]
    public Vector2 attackSize = new Vector2(0.9f, 0.6f);
    public float attackOffsetX = 0.6f;      // distance in front of the player
    public float attackDamage = 10f;
    public float attackKnockback = 6f;
    public float attackStartup = 0.05f;     // wind-up before hitbox
    public float attackActive = 0.10f;      // hitbox "live" window
    public float attackRecovery = 0.15f;    // lockout after the swing
    public float attackCooldown = 0.05f;    // time before next attack can start
    public LayerMask enemyLayer;

    private bool isAttacking;
    private int facingDir = 1;
    private float lastAttackTime = -10f;

    private Health health;

    private void Start()
    {
        playerCollider = GetComponent<BoxCollider2D>();
        originalSize = playerCollider.size;
        originalOffset = playerCollider.offset;
        health = GetComponent<Health>();
    }

    void Update()
    {
        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (isAttacking) speed = 0f;
        if (isAttacking || (health != null && health.IsInHitstun)) speed = 0f;
        rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        GroundCheck();
        Gravity();
        float current = spriteTransform.localScale.y;
        float next = Mathf.Lerp(current, targetScaleY,
                                Time.deltaTime * scaleLerpSpeed);
        spriteTransform.localScale = new Vector3(
            spriteTransform.localScale.x, next, spriteTransform.localScale.z);
    }

    private void Gravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallSpeedMuliplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;

        if (Mathf.Abs(horizontalMovement) > 0.01f)
            facingDir = horizontalMovement > 0 ? 1 : -1;
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (context.performed
            && !isAttacking
            && !isCrouching
            && isGrounded
            && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(PerformAttack());
        }
        if (health != null && health.IsInHitstun) return;
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // trigger animator here?: SetTrigger("Attack")

        yield return new WaitForSeconds(attackStartup);

        DoHitCheck();

        yield return new WaitForSeconds(attackActive);
        yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
    }

    private void DoHitCheck()
    {
        Vector2 center = (Vector2)transform.position
                         + new Vector2(facingDir * attackOffsetX, 0f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackSize, 0f, enemyLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var target = hit.GetComponent<IDamageable>();
            if (target == null) continue;

            Vector2 knockback = new Vector2(facingDir, 0.35f).normalized * attackKnockback;
            target.TakeDamage(attackDamage, knockback);
        }
    }

    public void Drop (InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isAttacking)
        {
            float timeSinceLast = Time.time - lastDownPressTime;
            lastDownPressTime = Time.time;

            if (timeSinceLast <= doubleTapWindow && isOnPlatform && playerCollider.enabled)
            {
                // Double tap on a platform → drop through
                SetCrouch(false);
                StartCoroutine(DisablePlayerCollider(0.25f));
            }
            else
            {
                // Single tap → crouch
                SetCrouch(true);
            }
        }
        else if (context.canceled)
        {
            SetCrouch(false);
        }
        if (health != null && health.IsInHitstun) return;
    }

    private void SetCrouch(bool crouch)
    {
        isCrouching = crouch;
        targetScaleY = crouch ? crouchScaleY : 1f;

        if (crouch)
        {
            playerCollider.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            playerCollider.offset = new Vector2(originalOffset.x, originalOffset.y - originalSize.y * 0.25f);
        }
        else
        {
            // Check for room above before standing up
            if (Physics2D.OverlapBox(
                    (Vector2)transform.position + originalOffset,
                    originalSize, 0f, groundLayer))
            {
                isCrouching = true;
                return;
            }
            playerCollider.size = originalSize;
            playerCollider.offset = originalOffset;
        }
    }

 private IEnumerator DisablePlayerCollider (float disableTime)
    {
        playerCollider.enabled = false;
        yield return new WaitForSeconds(disableTime);
        while (Physics2D.OverlapBox(playerCollider.bounds.center, playerCollider.bounds.size, 0f, groundLayer))
            yield return null;
        playerCollider.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatform = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatform = false;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (jumpsRemaining > 0)
        {
            if (context.performed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                jumpsRemaining--;
            }
            else if (context.canceled)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
                jumpsRemaining--;
            }
        }
        if (isCrouching) return;
        if (health != null && health.IsInHitstun) return;
    }

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPos != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        }

        Gizmos.color = Color.red;
        Vector2 center = (Vector2)transform.position
                         + new Vector2(facingDir * attackOffsetX, 0f);
        Gizmos.DrawWireCube(center, attackSize);
    }
}
