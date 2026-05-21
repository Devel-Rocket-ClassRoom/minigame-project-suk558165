using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
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
    public WeaponInventory weaponInventory;

    [Header("Ranged Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 12f;
    public float rangedDamage = 15f;
    public float rangedCooldown = 0.8f;

    [Header("Debug")]
    public bool previewDeath;

    private PlayerHealth health;

    private Rigidbody2D rb;
    private Animator animator;
    private AnimatorOverrideController overrideController;
    private SpriteRenderer sr;
    private Transform visuals;

    private bool isGrounded;
    private float moveInput;
    private int jumpCharges;
    private bool wasGrounded;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashCharges;
    private float attackTimer;
    private float attackCooldown = 0.5f;
    private bool isAttacking;
    private float rangedAttackTimer;
    private bool deathHandled;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashSwordAttack = Animator.StringToHash("SwordAttack");
    private static readonly int HashBowAttack = Animator.StringToHash("BowAttack");
    private static readonly int HashDash = Animator.StringToHash("Dash");
    private static readonly int HashIsDead = Animator.StringToHash("IsDead");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        visuals = transform.Find("Visuals");

        rb.gravityScale = gravityScale;
        dashCharges = maxDashCharges;
        jumpCharges = maxJumpCharges;

        health = GetComponent<PlayerHealth>();

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        if (weaponInventory != null)
            weaponInventory.OnWeaponChanged += ApplyWeapon;
    }

    void Start()
    {
        if (weaponInventory != null && weaponInventory.Current != null)
            ApplyWeapon(weaponInventory.Current);
    }

    void OnDestroy()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponChanged -= ApplyWeapon;
    }

    void ApplyWeapon(WeaponData data)
    {
        if (data.attackClip != null)
            overrideController["Wolf_SwordAttack"] = data.attackClip;

        if (weapon != null)
            weapon.ApplyWeaponData(data);

        attackCooldown = data.attackCooldown;
    }

    void Update()
    {
        bool dead = (health != null && health.IsDead) || previewDeath;
        if (dead)
        {
            animator.SetBool(HashIsDead, true);
            if (!deathHandled)
            {
                deathHandled = true;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
                StartCoroutine(ReloadAfterDelay(3f));
            }
            return;
        }

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
        rangedAttackTimer -= Time.deltaTime;
        isAttacking = false;

        if (Input.GetKeyDown(KeyCode.X) && weapon != null)
        {
            isAttacking = true;
            bool facingLeftNow =
                visuals != null ? visuals.localScale.x < 0f : (sr != null && sr.flipX);
            Vector3 fakeTarget =
                transform.position + (facingLeftNow ? Vector3.left : Vector3.right) * 5f;
            weapon.Attack(fakeTarget);
            animator.Play("SwordAttack", 0, 0f);
        }

        if (Input.GetKeyDown(KeyCode.C) && rangedAttackTimer <= 0f && projectilePrefab != null)
        {
            rangedAttackTimer = rangedCooldown;
            isAttacking = true;
            animator.Play("BowAttack", 0, 0f);
            ShootForward();
        }

        animator.SetFloat(HashSpeed, Mathf.Abs(moveInput) > 0f ? 1f : 0f);
        animator.SetBool(HashIsGrounded, isGrounded || isDashing);

        if (!isAttacking)
        {
            if (moveInput > 0f)
                Flip(false);
            else if (moveInput < 0f)
                Flip(true);
        }
    }

    void ShootForward()
    {
        bool facingLeft = visuals != null ? visuals.localScale.x < 0f : (sr != null && sr.flipX);
        Vector2 dir = facingLeft ? Vector2.left : Vector2.right;
        Vector3 origin = transform.position + (Vector3)(dir * 0.7f);
        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Init(dir, projectileSpeed, rangedDamage, gameObject);
    }

    private void Flip(bool flipX)
    {
        if (visuals != null)
        {
            Vector3 scale = visuals.localScale;
            scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            visuals.localScale = scale;
        }
        else if (sr != null)
        {
            sr.flipX = flipX;
        }
    }

    void FixedUpdate()
    {
        float speed = isDashing ? walkSpeed * dashSpeedMultiplier : walkSpeed;
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity +=
                Vector2.up
                * Physics2D.gravity.y
                * (fallGravityMultiplier - 1f)
                * Time.fixedDeltaTime;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    IEnumerator ReloadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
