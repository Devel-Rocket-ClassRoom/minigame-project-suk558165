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
    public float rangedDamage = 15f;
    public float rangedCooldown = 0.8f;

    [Header("Attack Settings")]
    [SerializeField]
    private string swordAttackStateName = "SwordAttack";

    [SerializeField]
    private string bowAttackStateName = "BowAttack";

    [SerializeField]
    private string swordOverrideClipName = "Player_SwordAttack";

    [SerializeField]
    private string idleOverrideClipName = "Player_Idle";

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

    private Animator animator;
    private AnimatorOverrideController overrideController;
    private PlayerMovement movement;
    private float rangedAttackTimer;

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
        rangedAttackTimer -= Time.deltaTime;
        IsAttacking = false;

        if (InventoryUI.IsOpen)
            return;

        var currentWeapon = weaponInventory != null ? weaponInventory.Current : null;
        if (!Input.GetKeyDown(KeyCode.X) || currentWeapon == null)
            return;
        if (!movement.IsGrounded && movement.AirAttackUsed)
            return;

        Vector2 attackDir = GetAttackDirection();

        if (currentWeapon.weaponType == WeaponType.Melee && weapon != null)
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            animator.Play(swordAttackStateName, 0, 0f);
        }
        else if (
            currentWeapon.weaponType == WeaponType.Ranged
            && rangedAttackTimer <= 0f
            && projectilePrefab != null
        )
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            rangedAttackTimer = rangedCooldown;
            animator.Play(bowAttackStateName, 0, 0f);
            ShootInDirection(attackDir);
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

        // 방향키 없으면 바라보는 방향
        bool facingLeft =
            movement.Visuals != null
                ? movement.Visuals.localScale.x < 0f
                : (movement.Sr != null && movement.Sr.flipX);
        return facingLeft ? Vector2.left : Vector2.right;
    }

    void ShootInDirection(Vector2 dir)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Init(dir, projectileSpeed, rangedDamage, gameObject);
    }

    void ApplyWeapon(WeaponData data)
    {
        AnimationClip attackClip =
            data.weaponType == WeaponType.Ranged ? rangedAttackClip : meleeAttackClip;
        AnimationClip idleClip =
            data.weaponType == WeaponType.Ranged ? rangedIdleClip : meleeIdleClip;

        if (attackClip != null)
            overrideController[swordOverrideClipName] = attackClip;

        if (idleClip != null)
        {
            overrideController[idleOverrideClipName] = idleClip;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle"))
                animator.Play("Idle", 0, stateInfo.normalizedTime);
        }

        if (weapon != null)
            weapon.ApplyWeaponData(data);
    }
}
