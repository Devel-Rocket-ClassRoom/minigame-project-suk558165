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

    [Header("Air Control")]
    [Tooltip("공중에서 목표 속도에 도달하는 가속 (값이 클수록 즉각 반응, 작을수록 관성 유지)")]
    public float airAcceleration = 40f;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;

    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip dashSound;

    [Header("Dash")]
    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    public int maxDashCharges = 2;

    public bool IsGrounded { get; private set; }
    public bool IsOnPlatform { get; private set; }
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

    private DashGhostEffect dashGhost;
    private Collider2D mainCollider;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Sr = GetComponent<SpriteRenderer>();
        Visuals = transform.Find("Visuals");
        inventory = GetComponent<Inventory>();

        dashGhost = GetComponent<DashGhostEffect>();
        mainCollider = GetComponent<Collider2D>();

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

        float left = Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f;
        float right = Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        MoveInput = left + right;

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

        if (pressingDown && IsOnPlatform)
        {
            StartCoroutine(DropDown());
            return;
        }

        if (jumpCharges > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, EffectiveJumpForce);
            jumpCharges--;
            AudioManager.Instance?.PlaySFX(jumpSound);
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
            AudioManager.Instance?.PlaySFX(dashSound);

            if (!IsGrounded)
                jumpCharges = 0;

            rb.gravityScale = 0f;
            dashGhost?.StartGhost();
        }

        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                IsDashing = false;
                rb.gravityScale = gravityScale;
                dashGhost?.StopGhost();
            }
        }
    }

    public void UpdateAnimatorAndFlip(bool isAttacking, bool flipAllowed = true)
    {
        animator.SetFloat(HashSpeed, Mathf.Abs(MoveInput) > 0f ? 1f : 0f);
        animator.SetBool(HashIsGrounded, IsGrounded || IsDashing);

        if (!isAttacking || flipAllowed)
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
            float targetX = MoveInput * EffectiveWalkSpeed;
            float newX;
            if (IsGrounded)
            {
                // 지상에서는 즉시 반응 (걷기 느낌 유지)
                newX = targetX;
            }
            else
            {
                // 공중에서는 가속도 기반으로 천천히 변화 → 관성 유지, "막힌 느낌" 제거
                newX = Mathf.MoveTowards(
                    rb.linearVelocity.x,
                    targetX,
                    airAcceleration * Time.fixedDeltaTime
                );
            }
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

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

        IsOnPlatform = hit;
    }

    IEnumerator DropDown()
    {
        if (isDropping)
            yield break;

        // 발 근처의 플랫폼 중, 플레이어 발보다 위로 살짝이라도 솟아있지 않은 — 즉 발이 올라타 있는 — 플랫폼만 통과시킨다.
        // 발 아래 멀리 있는 다른 플랫폼(중간층)은 통과 대상에서 제외해야 아랫점프 후 그 위에 착지할 수 있다.
        float feetY = mainCollider != null ? mainCollider.bounds.min.y : groundCheck.position.y;
        var hits = Physics2D.OverlapCircleAll(groundCheck.position, 0.3f, platformLayer);
        var toIgnore = new System.Collections.Generic.List<Collider2D>();
        foreach (var h in hits)
        {
            if (h == null)
                continue;
            // 플랫폼 상단이 발 높이보다 약간 위/같은 높이일 때만 — 즉 현재 올라타 있는 플랫폼만 통과
            if (h.bounds.max.y <= feetY + 0.2f)
                toIgnore.Add(h);
        }
        if (toIgnore.Count == 0)
            yield break;

        isDropping = true;
        foreach (var p in toIgnore)
            Physics2D.IgnoreCollision(mainCollider, p, true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -10f);

        yield return new WaitForSeconds(dropDownDuration);

        foreach (var p in toIgnore)
            if (p != null)
                Physics2D.IgnoreCollision(mainCollider, p, false);
        isDropping = false;
    }
}
