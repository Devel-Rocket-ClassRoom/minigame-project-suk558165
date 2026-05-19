using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class WolfController : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float jumpForce = 12f;
    public int maxJumpCharges = 2;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    public float gravityScale = 1.2f;
    public float fallGravityMultiplier = 1.2f;

    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    public int maxDashCharges = 2;

    [Header("Attack")]
    public PlayerWeapon weapon;
    public float attackCooldown = 0.5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private bool isGrounded;
    private float moveInput;
    private int jumpCharges;

    private bool wasGrounded;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashCharges;
    private float attackTimer;
    private bool isAttacking;

    [Header("Debug")]
    public bool previewDeath;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashBowAttack = Animator.StringToHash("BowAttack");
    private static readonly int HashSwordAttack = Animator.StringToHash("SwordAttack");
    private static readonly int HashDash = Animator.StringToHash("Dash");
    private static readonly int HashIsDead = Animator.StringToHash("IsDead");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = gravityScale;
        dashCharges = maxDashCharges;
        jumpCharges = maxJumpCharges;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput == 0f)
        {
            if (Input.GetKey(KeyCode.A))
                moveInput = -1f;
            else if (Input.GetKey(KeyCode.D))
                moveInput = 1f;
        }

        if (isGrounded && !wasGrounded)
            jumpCharges = maxJumpCharges;
        wasGrounded = isGrounded;

        if (Input.GetButtonDown("Jump") && jumpCharges > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCharges--;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // 대쉬 쿨타임
        if (dashCharges == 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
                dashCharges = maxDashCharges;
        }

        if (Input.GetKeyDown(KeyCode.Z) && !isDashing && dashCharges > 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCharges--;
            if (dashCharges == 0)
                dashCooldownTimer = dashCooldown;
            animator.SetTrigger(HashDash);
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        attackTimer -= Time.deltaTime;

        isAttacking = false;
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f && weapon != null)
        {
            attackTimer = attackCooldown;
            isAttacking = true;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            sr.flipX = mouseWorld.x < transform.position.x;
            animator.SetTrigger(HashSwordAttack);
            weapon.Attack(mouseWorld);
        }

        animator.SetBool(HashIsDead, previewDeath);
        animator.SetFloat(HashSpeed, Mathf.Abs(moveInput) > 0f ? 1f : 0f);
        animator.SetBool(HashIsGrounded, isGrounded || isDashing);

        if (!isAttacking)
        {
            if (moveInput > 0f)
                sr.flipX = false;
            else if (moveInput < 0f)
                sr.flipX = true;
        }
    }

    void FixedUpdate()
    {
        float currentSpeed = isDashing ? walkSpeed * dashSpeedMultiplier : walkSpeed;
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity +=
                Vector2.up
                * Physics2D.gravity.y
                * (fallGravityMultiplier - 1f)
                * Time.fixedDeltaTime;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
}
