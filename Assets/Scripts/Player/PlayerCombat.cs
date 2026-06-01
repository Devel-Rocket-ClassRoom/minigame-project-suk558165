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
    private Inventory inventory;
    private float attackTimer;

    // 원거리 발사 이벤트용 임시 저장
    private Vector2 pendingRangedDir;
    private float pendingRangedDamage;
    private StatBonus pendingRangedBonus;
    private bool hasPendingRanged;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        inventory = GetComponent<Inventory>();

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

        if (InventoryUI.IsOpen || ShopUI.IsOpen || PauseMenu.IsPaused)
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
        var bonus = inventory?.GetTotalStatBonus() ?? default;
        attackTimer = currentWeapon.attackCooldown / Mathf.Max(0.01f, 1f + bonus.attackSpeed);

        if (currentWeapon.weaponType == WeaponType.Melee && weapon != null)
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            animator.Play(SwordAttackState, 0, 0f);
            AudioManager.Instance?.PlaySFX(currentWeapon.attackSound);
        }
        else if (currentWeapon.weaponType == WeaponType.Ranged && projectilePrefab != null)
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            animator.Play(BowAttackState, 0, 0f);
            // 발사는 BowAttack 애니메이션 이벤트(OnRangedFire)에서 실행
            pendingRangedDir = attackDir;
            pendingRangedDamage = currentWeapon.damage;
            pendingRangedBonus = bonus;
            hasPendingRanged = true;
        }
    }

    Vector2 GetAttackDirection()
    {
        bool facingLeft =
            movement.Visuals != null
                ? movement.Visuals.localScale.x < 0f
                : (movement.Sr != null && movement.Sr.flipX);
        return facingLeft ? Vector2.left : Vector2.right;
    }

    void ShootInDirection(Vector2 dir, float baseDamage, StatBonus bonus)
    {
        float effectiveDamage = (baseDamage + bonus.damage) * (1f + bonus.damageDealtMult);
        if (bonus.criticalChance > 0f && Random.value < bonus.criticalChance)
            effectiveDamage *= 1f + bonus.criticalDamage;

        int totalArrows = bonus.arrowCount >= 2 ? bonus.arrowCount : 1;
        float perArrowDmg =
            totalArrows > 1 && bonus.arrowDamageMult > 0f
                ? effectiveDamage * bonus.arrowDamageMult
                : effectiveDamage;

        float spreadAngle = 15f;
        for (int i = 0; i < totalArrows; i++)
        {
            float offset = totalArrows > 1 ? (i - (totalArrows - 1) * 0.5f) * spreadAngle : 0f;
            SpawnProjectile(RotateVector(dir, offset), perArrowDmg, bonus.penetration);
        }
    }

    void SpawnProjectile(Vector2 dir, float damage, int pierce)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Init(dir, projectileSpeed, damage, gameObject, pierce: pierce);
    }

    static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad),
            sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Animation Event — Player_SwordAttack 히트 프레임 (frame 1, time 0.083s)
    public void OnMeleeHit()
    {
        weapon?.OnHitFrame();
    }

    // Animation Event — Player_BowAttack 발사 프레임 (frame 2, time 0.166s)
    public void OnRangedFire()
    {
        if (!hasPendingRanged)
            return;
        hasPendingRanged = false;
        var currentWeapon = weaponInventory?.Current;
        ShootInDirection(pendingRangedDir, pendingRangedDamage, pendingRangedBonus);
        AudioManager.Instance?.PlaySFX(currentWeapon?.attackSound);
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
