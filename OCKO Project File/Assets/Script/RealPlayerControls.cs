using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
//using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public class RealPlayerControls : MonoBehaviour
{
    public Rigidbody2D rb;

    [SerializeField] Animator animator;

	SpriteRenderer sprite;

	BoxCollider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    public Transform spriteTransform;

    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;

    [Header("Jump")]
    public float jumpPower = 10f;
    public int maxJumps = 2;
    int jumpsRemaining;

    [Header("Gound Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isOnPlatform;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMuliplier = 2f;

    public float crouchSpeedMultiplier = 0.5f;
    public float doubleTapWindow = 0.25f;
    private float lastDownPressTime = -10f;
    private bool isCrouching;

    private Vector2 originalSize;
    private Vector2 originalOffset;
    public float crouchScaleY = 0.5f;
    public float scaleLerpSpeed = 15f;      // smooth transition; ~10–20 feels good

    private float targetScaleY = 1f;

    [Header("Attack")]
    public float attackWidthFraction = 0.8f;
    public float attackHeightFraction = 0.8f;
    public float attackForwardGap = 0.05f;
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
    private Health healthComp;

    private Collider2D currentPlatform;
    private bool isDropping;
    private bool downHeld;
    private bool CanAct => healthComp == null
                        || (!healthComp.IsInHitstun && !healthComp.IsDead);

    private void Start()
    {
        playerCollider = GetComponent<BoxCollider2D>();
        originalSize = playerCollider.size;
        originalOffset = playerCollider.offset;
        healthComp = GetComponent<Health>();

        if (spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponentInChildren<SpriteRenderer>();

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isCrouching && !downHeld)
            SetCrouch(false);
        if (healthComp != null && (healthComp.IsInHitstun || healthComp.IsDead))
        {
            // still apply gravity so they don't float, but no horizontal input
            GroundCheck();
            Gravity();
            UpdateSpriteScale();
            return;
        }
        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (isAttacking || (healthComp != null && healthComp.IsInHitstun)) speed = 0f;
        if (isAttacking == true)
        {
            animator.SetBool("isAttacking", true);
        }
        else
        {
            animator.SetBool("isAttacking", false);
        }

        rb.linearVelocity = new Vector2(horizontalMovement * speed, rb.linearVelocity.y);

		GroundCheck();
        Gravity();
        UpdateSpriteScale();
	}

    private void FixedUpdate()
    {
		if (horizontalMovement != 0f)
		{
			sprite.flipX = horizontalMovement < 0;
			animator.SetBool("isMoving", true);
			Debug.Log(horizontalMovement);
		}
		else
		{
			Debug.Log(horizontalMovement);
			animator.SetBool("isMoving", false);
		}
	}

    private void UpdateSpriteScale()
    {
        if (spriteTransform == null) return;

        float current = spriteTransform.localScale.y;
        float next = Mathf.Lerp(current, targetScaleY, Time.deltaTime * scaleLerpSpeed);
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
        if (healthComp != null && healthComp.IsInHitstun) return;
        if (context.performed
            && !isAttacking
            && !isCrouching
            && isGrounded
            && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(PerformAttack());
        }

    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null) animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackStartup);

        DoHitCheck();

        yield return new WaitForSeconds(attackActive);
        yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;

    }

    private void DoHitCheck()
    {
        if (spriteRenderer == null) return;
        Bounds b = spriteRenderer.bounds;

        Vector2 hitboxSize = new Vector2(
            b.size.x * attackWidthFraction,
            b.size.y * attackHeightFraction
        );

        Vector2 hitboxCenter = new Vector2(
            b.center.x + facingDir * (b.extents.x + attackForwardGap + hitboxSize.x * 0.5f),
            b.center.y
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, 0f, enemyLayer);
      
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform))continue;

            var target = hit.GetComponent<IDamageable>();
            if (target == null)continue;

            Vector2 knockback = new Vector2(facingDir, 0.35f).normalized * attackKnockback;
            target.TakeDamage(attackDamage, knockback);
        }
    }

    public void Drop(InputAction.CallbackContext context)
    {
        if (context.performed) downHeld = true;
        else if (context.canceled) downHeld = false;

        if (context.canceled)
        {
            SetCrouch(false);
            return;
        }
        if (!CanAct) return;
        if (context.performed && isGrounded && !isAttacking)
        {
            float timeSinceLast = Time.time - lastDownPressTime;
            lastDownPressTime = Time.time;

            if (timeSinceLast <= doubleTapWindow && isOnPlatform && currentPlatform != null)
            {
                // Double tap on a platform → drop through
                SetCrouch(false);
                StartCoroutine(DropThroughPlatform(0.25f));
            }
            else
            {
                // Single tap → crouch
                SetCrouch(true);
            }
        }

        if (healthComp != null && healthComp.IsInHitstun) return;

    }

    private void SetCrouch(bool crouch)
    {
        if (crouch)
        {
            isCrouching = true;
            targetScaleY = crouchScaleY;
            playerCollider.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            playerCollider.offset = new Vector2(originalOffset.x,
                originalOffset.y - originalSize.y * 0.25f);
            return;
        }

        float headroomHeight = originalSize.y * 0.5f;
        Vector2 checkCenter = (Vector2)transform.position + new Vector2(originalOffset.x, originalOffset.y + headroomHeight * 0.5f);
        Vector2 checkSize = new Vector2(originalSize.x - 0.9f, headroomHeight * 0.9f);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundLayer);

        foreach (var c in overlaps)
        {
            if (c == null) continue;
            if (c.transform.IsChildOf(transform)) continue;
            Debug.Log($"Uncrouch blocked by: {c.name} on layer {LayerMask.LayerToName(c.gameObject.layer)}");
            return;                                          // real obstacle → stay crouched
        }

        isCrouching = false;
        targetScaleY = 1f;
        playerCollider.size = originalSize;
        playerCollider.offset = originalOffset;
}

    private IEnumerator DropThroughPlatform(float duration)
    {
        Collider2D platform = currentPlatform;
        if (platform == null) yield break;

        isDropping = true;
        Physics2D.IgnoreCollision(playerCollider, platform, true);
        yield return new WaitForSeconds(duration);

        if (platform != null)
            Physics2D.IgnoreCollision(playerCollider, platform, false);
        isDropping = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatform = true;
            currentPlatform = collision.collider;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatform = false;
            if (!isDropping)currentPlatform = null;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (isCrouching) return;
        if (healthComp != null && healthComp.IsInHitstun) return;
        if (jumpsRemaining > 0)
        {
            if (context.performed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                jumpsRemaining--;
                animator.SetTrigger("jump");
            }
            else if (context.canceled)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
        }
       
    }

    private void GroundCheck()
    {
        if (groundCheckPos == null) return;

        isGrounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPos == null) return;
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

        if (spriteRenderer == null && spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null) return;

        Bounds b = spriteRenderer.bounds;
        Vector2 hitboxSize = new Vector2(b.size.x * attackWidthFraction, b.size.y * attackHeightFraction);
        Vector2 hitboxCenter = new Vector2(
            b.center.x + facingDir * (b.extents.x + attackForwardGap + hitboxSize.x * 0.5f),
            b.center.y
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitboxCenter, hitboxSize);
    }

    public void ResetForRespawn()
    {
        rb.linearVelocity = Vector2.zero;

        isAttacking = false;
        isCrouching = false;
        isGrounded = false;
        isOnPlatform = false;

        jumpsRemaining = maxJumps;
        horizontalMovement = 0f;
        lastDownPressTime = -10f;

        playerCollider.size = originalSize;
        playerCollider.offset = originalOffset;
        playerCollider.enabled = true;

        targetScaleY = 1f;

        if (spriteTransform != null)
            spriteTransform.localScale = new Vector3(spriteTransform.localScale.x, 1f, spriteTransform.localScale.z);
    }



}
