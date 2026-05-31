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
    private Inventory inventory;

    private float baseWalkSpeed;
    private float baseJumpForce;
    private int baseDashCharges;
    private float baseDashDuration;

    float EffectiveWalkSpeed
    {
        get
        {
            var b = inventory?.GetTotalStatBonus() ?? default;
            return baseWalkSpeed * (1f + b.speed);
        }
    }
    float EffectiveJumpForce
    {
        get
        {
            var b = inventory?.GetTotalStatBonus() ?? default;
            return baseJumpForce * (1f + b.jump);
        }
    }
    int EffectiveDashCharges
    {
        get
        {
            var b = inventory?.GetTotalStatBonus() ?? default;
            return baseDashCharges + b.dashCount;
        }
    }
    float EffectiveDashDuration
    {
        get
        {
            var b = inventory?.GetTotalStatBonus() ?? default;
            return baseDashDuration * (1f + b.dashRange);
        }
    }

    private int jumpCharges;
    private bool wasGrounded;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashCharges;
    private float dashDirection;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    private bool isDropping;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashDash = Animator.StringToHash("Dash");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Sr = GetComponent<SpriteRenderer>();
        Visuals = transform.Find("Visuals");
        inventory = GetComponent<Inventory>();

        baseWalkSpeed = walkSpeed;
        baseJumpForce = jumpForce;
        baseDashCharges = maxDashCharges;
        baseDashDuration = dashDuration;

        rb.gravityScale = gravityScale;
        dashCharges = maxDashCharges;
        jumpCharges = maxJumpCharges;

        isDropping = false;
    }

    public void HandleInput()
    {
        if (InventoryUI.IsOpen || ShopUI.IsOpen || PauseMenu.IsPaused)
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

        bool pressingDown = Input.GetAxisRaw("Vertical") < 0f;

        if (pressingDown && IsGrounded)
        {
            StartCoroutine(DropDown());
            return;
        }

        if (jumpCharges > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, EffectiveJumpForce);
            jumpCharges--;
        }
    }

    void HandleDash()
    {
        if (dashCharges == 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
                dashCharges = EffectiveDashCharges;
        }

        var dashKey = InputManager.Instance?.Dash ?? KeyCode.Z;
        if (Input.GetKeyDown(dashKey) && !IsDashing && dashCharges > 0)
        {
            bool facingLeft =
                Visuals != null ? Visuals.localScale.x < 0f : (Sr != null && Sr.flipX);
            dashDirection = facingLeft ? -1f : 1f;

            IsDashing = true;
            dashTimer = EffectiveDashDuration;
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
            rb.linearVelocity = new Vector2(
                dashDirection * EffectiveWalkSpeed * dashSpeedMultiplier,
                0f
            );
        }
        else
        {
            // 아랫점프 중이면 FixedUpdate에서 속도를 강제 적용 (코루틴에서 설정한 속도가 물리 보정에 덮어써지는 문제 방지)
            float yVel = isDropping ? Mathf.Min(rb.linearVelocity.y, -10f) : rb.linearVelocity.y;
            rb.linearVelocity = new Vector2(MoveInput * EffectiveWalkSpeed, yVel);

            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity +=
                    Vector2.up
                    * Physics2D.gravity.y
                    * (fallGravityMultiplier - 1f)
                    * Time.fixedDeltaTime;
        }

        // 상승 중이거나 아랫점프 중이면 플랫폼 레이어 무시, 그 외엔 복원
        int platLayer = LayerMaskToIndex(platformLayer);
        if (platLayer >= 0)
        {
            bool ignorePlatform = isDropping || rb.linearVelocity.y > 0.5f;
            Physics2D.IgnoreLayerCollision(gameObject.layer, platLayer, ignorePlatform);
        }

        Vector2 checkPos = groundCheck.position;
        bool falling = rb.linearVelocity.y < -0.5f;
        float halfW = falling ? groundCheckSize.x * 0.5f : groundCheckSize.x * 0.27f;
        LayerMask combinedLayer = isDropping ? groundLayer : (groundLayer | platformLayer);
        bool hit =
            Physics2D.OverlapCircle(checkPos, 0.15f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.left * halfW, 0.12f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.right * halfW, 0.12f, combinedLayer);
        IsGrounded = hit && rb.linearVelocity.y <= 1.0f;
    }

    IEnumerator DropDown()
    {
        if (isDropping)
            yield break;

        isDropping = true;

        var col = GetComponent<Collider2D>();
        col.enabled = false;
        rb.position += Vector2.down * 0.5f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -8f);

        yield return new WaitForSeconds(0.15f);

        col.enabled = true;

        yield return new WaitForSeconds(0.25f);

        isDropping = false;
    }

    static int LayerMaskToIndex(LayerMask mask)
    {
        for (int i = 0; i < 32; i++)
            if ((mask.value & (1 << i)) != 0)
                return i;
        return -1;
    }
}
