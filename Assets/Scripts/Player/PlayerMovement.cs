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
            // 단, 이미 다른 플랫폼/지면에 착지했다면 강제 속도를 풀어서 통과해 버리는 문제 방지
            float yVel =
                (isDropping && !IsGrounded)
                    ? Mathf.Min(rb.linearVelocity.y, -10f)
                    : rb.linearVelocity.y;
            rb.linearVelocity = new Vector2(MoveInput * EffectiveWalkSpeed, yVel);

            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity +=
                    Vector2.up
                    * Physics2D.gravity.y
                    * (fallGravityMultiplier - 1f)
                    * Time.fixedDeltaTime;
        }

        // 점프 상승 중엔 플랫폼 충돌 제외 (아랫점프는 DropDown에서 콜라이더를 직접 끔)
        // 점프 정점에서 플레이어가 플랫폼 내부에 있으면 끼임이 발생하므로, 겹치는 동안에는 계속 제외 유지
        if (mainCollider != null)
        {
            bool overlappingPlatform = Physics2D.OverlapBox(
                mainCollider.bounds.center,
                mainCollider.bounds.size * 0.95f,
                0f,
                platformLayer
            );
            bool goingUp = rb.linearVelocity.y > 0.5f;
            mainCollider.excludeLayers =
                (goingUp || overlappingPlatform) ? platformLayer : (LayerMask)0;
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

        // 속도 조건 없이 순수 overlap — groundLayer·platformLayer 모두 체크해 아랫점프 입력 수신에 사용
        IsOnPlatform =
            Physics2D.OverlapCircle(checkPos, 0.15f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.left * halfW, 0.12f, combinedLayer)
            || Physics2D.OverlapCircle(checkPos + Vector2.right * halfW, 0.12f, combinedLayer);
    }

    IEnumerator DropDown()
    {
        if (isDropping)
            yield break;

        // 발 아래 platformLayer 콜라이더 찾기
        Collider2D platformCol = Physics2D.OverlapCircle(groundCheck.position, 0.5f, platformLayer);
        if (platformCol == null)
            yield break;

        isDropping = true;
        platformCol.enabled = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -10f);

        yield return new WaitForSeconds(dropDownDuration);

        if (platformCol != null)
            platformCol.enabled = true;

        isDropping = false;
    }
}
