using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class WolfController : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 12f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private bool isGrounded;
    private float moveInput;
    private bool isRunning;

    private float lastTapTime;
    private float lastTapDir;
    private const float doubleTapWindow = 0.3f;

    private bool suppressJump;
    private Coroutine attackRoutine;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashBowAttack = Animator.StringToHash("BowAttack");
    private static readonly int HashSwordAttack = Animator.StringToHash("SwordAttack");
    private static readonly int HashIsDead = Animator.StringToHash("IsDead");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 방향키 두 번 탭으로 달리기
        float dir = 0f;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            dir = 1f;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            dir = -1f;

        if (dir != 0f)
        {
            if (dir == lastTapDir && Time.time - lastTapTime < doubleTapWindow)
                isRunning = true;
            lastTapDir = dir;
            lastTapTime = Time.time;
        }

        // 방향 전환하거나 멈추면 달리기 해제
        if (
            moveInput == 0f
            || (moveInput > 0f && lastTapDir < 0f)
            || (moveInput < 0f && lastTapDir > 0f)
        )
            isRunning = false;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            animator.SetTrigger(HashBowAttack);
            if (!isGrounded)
                StartAirAttack();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            animator.SetTrigger(HashSwordAttack);
            if (!isGrounded)
                StartAirAttack();
        }

        float absInput = Mathf.Abs(moveInput);
        float speed = absInput > 0f ? (isRunning ? 1f : 0.5f) : 0f;
        animator.SetFloat(HashSpeed, speed);
        animator.SetBool(HashIsGrounded, isGrounded || suppressJump);

        if (moveInput > 0f)
            sr.flipX = false;
        else if (moveInput < 0f)
            sr.flipX = true;
    }

    void FixedUpdate()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void StartAirAttack()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(SuppressJumpDuringAttack());
    }

    System.Collections.IEnumerator SuppressJumpDuringAttack()
    {
        suppressJump = true;
        // 트리거가 적용되어 애니메이터가 공격 상태로 전환될 때까지 대기
        yield return null;
        yield return null;
        // 공격 상태가 끝날 때까지 대기
        while (true)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            bool inAttack = info.IsName("Attack") || info.IsName("SwordAttack");
            if (!inAttack || info.normalizedTime >= 1f)
                break;
            yield return null;
        }
        suppressJump = false;
        attackRoutine = null;
    }
}
