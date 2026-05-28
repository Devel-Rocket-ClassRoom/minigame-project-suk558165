using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float jumpForce = 17f;
    public int maxJumpCharges = 2;
    public LayerMask groundLayer;
    public LayerMask platformLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.65f, 0.3f);
    public float dropDownDuration = 0.15f;

    [Header("Gravity")]
    public float gravityScale = 4f;
    public float fallGravityMultiplier = 2.5f;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;

    [Header("Dash")]
    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    public int maxDashCharges = 2;

    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }
    public float MoveInput { get; private set; }
    public Transform Visuals { get; private set; }
    public SpriteRenderer Sr { get; private set; }
    public bool AirAttackUsed { get; set; }

    private Rigidbody2D rb;
    private Animator animator;

    private int jumpCharges;
    private bool wasGrounded;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashCharges;
    private float dashDirection;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashDash = Animator.StringToHash("Dash");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Sr = GetComponent<SpriteRenderer>();
        Visuals = transform.Find("Visuals");

        rb.gravityScale = gravityScale;
        dashCharges = maxDashCharges;
        jumpCharges = maxJumpCharges;
    }

    public void HandleInput()
    {
        if (InventoryUI.IsOpen || PauseMenu.IsPaused)
        {
            MoveInput = 0f;
            return;
        }

        MoveInput = Input.GetAxisRaw("Horizontal");

        if (IsGrounded && !wasGrounded)
        {
            jumpCharges = maxJumpCharges;
            AirAttackUsed = false;
        }
        wasGrounded = IsGrounded;

        HandleJump();
        HandleDash();
    }

    void HandleJump()
    {
        if (!Input.GetButtonDown("Jump"))
            return;

        bool pressingDown = Input.GetKey(KeyCode.DownArrow);
        Vector2 pCheckPos = groundCheck.position;
        float pHalfW = groundCheckSize.x * 0.5f;
        bool onPlatform =
            platformLayer.value != 0
            && (
                Physics2D.OverlapCircle(pCheckPos, 0.15f, platformLayer)
                || Physics2D.OverlapCircle(pCheckPos + Vector2.left * pHalfW, 0.12f, platformLayer)
                || Physics2D.OverlapCircle(pCheckPos + Vector2.right * pHalfW, 0.12f, platformLayer)
            );

        if (pressingDown && onPlatform)
        {
            StartCoroutine(DropDown());
        }
        else if (jumpCharges > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCharges--;
        }
    }

    void HandleDash()
    {
        if (dashCharges == 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
                dashCharges = maxDashCharges;
        }

        if (Input.GetKeyDown(KeyCode.Z) && !IsDashing && dashCharges > 0)
        {
            bool facingLeft =
                Visuals != null ? Visuals.localScale.x < 0f : (Sr != null && Sr.flipX);
            dashDirection = facingLeft ? -1f : 1f;

            IsDashing = true;
            dashTimer = dashDuration;
            dashCharges--;
            if (dashCharges == 0)
                dashCooldownTimer = dashCooldown;

            if (!IsGrounded)
                jumpCharges = 0;

            rb.gravityScale = 0f;
            animator.SetTrigger(HashDash);
        }

        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                IsDashing = false;
                rb.gravityScale = gravityScale;
            }
        }
    }

    public void UpdateAnimatorAndFlip(bool isAttacking)
    {
        animator.SetFloat(HashSpeed, Mathf.Abs(MoveInput) > 0f ? 1f : 0f);
        animator.SetBool(HashIsGrounded, IsGrounded || IsDashing);

        if (!isAttacking)
        {
            if (MoveInput > 0f)
                Flip(false);
            else if (MoveInput < 0f)
                Flip(true);
        }
    }

    public void Flip(bool flipLeft)
    {
        if (Visuals != null)
        {
            Vector3 scale = Visuals.localScale;
            scale.x = flipLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            Visuals.localScale = scale;
        }

        if (Sr != null)
            Sr.flipX = flipLeft;
    }

    public void ApplyKnockback(Vector2 velocity)
    {
        knockbackVelocity = velocity;
        knockbackTimer = knockbackDuration;
    }

    public void FixedUpdateMovement()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        if (IsDashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * walkSpeed * dashSpeedMultiplier, 0f);
        }
        else
        {
            rb.linearVelocity = new Vector2(MoveInput * walkSpeed, rb.linearVelocity.y);

            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity +=
                    Vector2.up
                    * Physics2D.gravity.y
                    * (fallGravityMultiplier - 1f)
                    * Time.fixedDeltaTime;
        }

        Vector2 checkPos = groundCheck.position;
        bool falling = rb.linearVelocity.y < -0.5f;
        float halfW = falling ? groundCheckSize.x * 0.5f : groundCheckSize.x * 0.27f;
        LayerMask combinedLayer = groundLayer | platformLayer;
        bool hit =
            Physics2D.OverlapCircle(checkPos, 0.15f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.left * halfW, 0.12f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.right * halfW, 0.12f, combinedLayer);
        IsGrounded = hit && rb.linearVelocity.y <= 1.0f;
    }

    IEnumerator DropDown()
    {
        var playerCol = GetComponent<Collider2D>();
        Vector2 dCheckPos = groundCheck.position;
        float dHalfW = groundCheckSize.x * 0.5f;
        var hitSet = new System.Collections.Generic.HashSet<Collider2D>();
        foreach (var c in Physics2D.OverlapCircleAll(dCheckPos, 0.15f, platformLayer))
            hitSet.Add(c);
        foreach (
            var c in Physics2D.OverlapCircleAll(
                dCheckPos + Vector2.left * dHalfW,
                0.12f,
                platformLayer
            )
        )
            hitSet.Add(c);
        foreach (
            var c in Physics2D.OverlapCircleAll(
                dCheckPos + Vector2.right * dHalfW,
                0.12f,
                platformLayer
            )
        )
            hitSet.Add(c);
        var hits = new Collider2D[hitSet.Count];
        hitSet.CopyTo(hits);
        if (hits.Length == 0)
            yield break;

        foreach (var c in hits)
            Physics2D.IgnoreCollision(playerCol, c, true);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -6f);

        yield return new WaitForSeconds(dropDownDuration);

        foreach (var c in hits)
            if (c != null)
                Physics2D.IgnoreCollision(playerCol, c, false);
    }
}
