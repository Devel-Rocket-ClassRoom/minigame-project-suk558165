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

    [Header("Attack Clips")]
    [SerializeField]
    private AnimationClip meleeAttackClip;

    [SerializeField]
    private AnimationClip rangedAttackClip;

    [SerializeField]
    private AnimationClip magicAttackClip;

    public bool IsAttacking { get; private set; }

    private const string SwordAttackState = "SwordAttack";
    private const string BowAttackState = "BowAttack";
    private const string MagicAttackState = "MagicAttack";
    private const string SwordOverrideClip = "Player_SwordAttack";
    private const string BowOverrideClip = "Player_BowAttack";
    private const string IdleOverrideClip = "Player_Idle";
    private const string WalkOverrideClip = "Player_Walk";

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

        if (IsAttacking)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool inAttack =
                stateInfo.IsName(SwordAttackState)
                || stateInfo.IsName(BowAttackState)
                || stateInfo.IsName(MagicAttackState);
            if (!inAttack)
            {
                IsAttacking = false;
                weapon?.Hide();
            }
        }

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
            weapon.Show();
            animator.Play(SwordAttackState, 0, 0f);
            AudioManager.Instance?.PlaySFX(currentWeapon.attackSound);
        }
        else if (
            (
                currentWeapon.weaponType == WeaponType.Ranged
                || currentWeapon.weaponType == WeaponType.Magic
            )
            && projectilePrefab != null
        )
        {
            if (!movement.IsGrounded)
                movement.AirAttackUsed = true;
            IsAttacking = true;
            weapon?.Show();
            string attackState =
                currentWeapon.weaponType == WeaponType.Magic ? MagicAttackState : BowAttackState;
            animator.Play(attackState, 0, 0f);
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

        float spreadAngle = 30f;
        for (int i = 0; i < totalArrows; i++)
        {
            float offset = totalArrows > 1 ? (i - (totalArrows - 1) * 0.5f) * spreadAngle : 0f;
            SpawnProjectile(RotateVector(dir, offset), perArrowDmg, bonus.penetration);
        }
    }

    void SpawnProjectile(Vector2 dir, float damage, int pierce)
    {
        Vector3 origin = transform.position;
        if (firePoint != null)
        {
            origin = firePoint.position;
            // firePoint가 Visuals 계층 밖에 있으면 좌향 시 X가 반전되지 않으므로 보정
            float xDiff = firePoint.position.x - transform.position.x;
            bool facingLeft = dir.x < 0f;
            bool firePointOnRight = xDiff > 0.001f;
            if (facingLeft == firePointOnRight)
                origin.x = transform.position.x - xDiff;
        }
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

    // Animation Event — 공격 애니메이션 마지막 프레임에서 호출
    public void OnAttackEnd()
    {
        weapon?.Hide();
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
        if (data == null)
            return;

        // Magic은 컨트롤러에 MagicAttack 상태로 직접 배치돼 있어 override 불필요.
        if (data.weaponType != WeaponType.Magic)
        {
            AnimationClip attackClip = data.weaponType switch
            {
                WeaponType.Ranged => rangedAttackClip,
                _ => meleeAttackClip,
            };

            if (attackClip != null)
            {
                string overrideKey =
                    data.weaponType == WeaponType.Ranged ? BowOverrideClip : SwordOverrideClip;
                overrideController[overrideKey] = attackClip;
            }
        }

        if (weapon != null)
            weapon.ApplyWeaponData(data);
    }
}
