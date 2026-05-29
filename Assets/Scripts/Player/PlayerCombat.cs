using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee")]
    public PlayerWeapon weapon;
    public WeaponInventory weaponInventory;

    [Header("Ranged")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 12f;

    [Header("Sword Animator")]
    [SerializeField]
    private Animator swordAnimator;

    [Header("Type-based Clips")]
    [SerializeField]
    private AnimationClip meleeAttackClip;

    [SerializeField]
    private AnimationClip meleeIdleClip;

    [SerializeField]
    private AnimationClip rangedAttackClip;

    [SerializeField]
    private AnimationClip rangedIdleClip;

    public bool IsAttacking { get; private set; }

    private const string SwordAttackState = "SwordAttack";
    private const string BowAttackState = "BowAttack";
    private const string SwordOverrideClip = "Player_SwordAttack";
    private const string IdleOverrideClip = "Player_Idle";

    private Animator animator;
    private AnimatorOverrideController overrideController;
    private PlayerMovement movement;
    private float attackTimer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();

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

    public void HandleInput()
    {
        attackTimer -= Time.deltaTime;
        IsAttacking = false;

        if (InventoryUI.IsOpen || PauseMenu.IsPaused)
            return;

        var currentWeapon = weaponInventory != null ? weaponInventory.Current : null;
        var attackKey = InputManager.Instance?.Attack ?? KeyCode.X;
        if (!Input.GetKeyDown(attackKey) || currentWeapon == null)
            return;
        if (attackTimer > 0f)
            return;
        if (!movement.IsGrounded && movement.AirAttackUsed)
            return;

        Vector2 attackDir = GetAttackDirection();
        attackTimer = currentWeapon.attackCooldown;

        if (currentWeapon.weaponType == WeaponType.Melee && weapon != null)
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            animator.Play(SwordAttackState, 0, 0f);
            swordAnimator?.SetTrigger("Attack");
            StartCoroutine(MeleeHitAfterDelay(0.15f));
        }
        else if (currentWeapon.weaponType == WeaponType.Ranged && projectilePrefab != null)
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            animator.Play(BowAttackState, 0, 0f);
            ShootInDirection(attackDir, currentWeapon.damage);
        }
    }

    Vector2 GetAttackDirection()
    {
        float h = 0f,
            v = 0f;
        if (Input.GetKey(KeyCode.RightArrow))
            h = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow))
            h = -1f;

        if (Input.GetKey(KeyCode.UpArrow))
            v = 1f;
        else if (Input.GetKey(KeyCode.DownArrow))
            v = -1f;

        if (h != 0f || v != 0f)
            return new Vector2(h, v).normalized;

        bool facingLeft =
            movement.Visuals != null
                ? movement.Visuals.localScale.x < 0f
                : (movement.Sr != null && movement.Sr.flipX);
        return facingLeft ? Vector2.left : Vector2.right;
    }

    void ShootInDirection(Vector2 dir, float damage)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Init(dir, projectileSpeed, damage, gameObject);
    }

    IEnumerator MeleeHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        weapon?.OnHitFrame();
    }

    void ApplyWeapon(WeaponData data)
    {
        AnimationClip attackClip =
            data.weaponType == WeaponType.Ranged ? rangedAttackClip : meleeAttackClip;
        AnimationClip idleClip =
            data.weaponType == WeaponType.Ranged ? rangedIdleClip : meleeIdleClip;

        if (attackClip != null)
            overrideController[SwordOverrideClip] = attackClip;

        if (idleClip != null)
        {
            overrideController[IdleOverrideClip] = idleClip;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle"))
                animator.Play("Idle", 0, stateInfo.normalizedTime);
        }

        if (weapon != null)
            weapon.ApplyWeaponData(data);
    }
}
