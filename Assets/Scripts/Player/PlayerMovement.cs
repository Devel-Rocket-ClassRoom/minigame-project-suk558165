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
    public float groundCheckRadius = 0.2f;
    public float dropDownDuration = 0.3f;

    [Header("Gravity")]
    public float gravityScale = 4f;
    public float fallGravityMultiplier = 2.5f;

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
        bool onPlatform =
            platformLayer.value != 0
            && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, platformLayer);

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
        else if (Sr != null)
        {
            Sr.flipX = flipLeft;
        }
    }

    public void FixedUpdateMovement()
    {
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

        bool hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        IsGrounded = hit && rb.linearVelocity.y <= 0.1f;
    }

    IEnumerator DropDown()
    {
        var platGO = GameObject.Find("Tilemap_Platform");
        if (platGO == null)
            yield break;

        var cols = platGO.GetComponents<Collider2D>();
        foreach (var c in cols)
            c.enabled = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -6f);

        yield return new WaitForSeconds(dropDownDuration);

        foreach (var c in cols)
            if (c != null)
                c.enabled = true;
    }
}
