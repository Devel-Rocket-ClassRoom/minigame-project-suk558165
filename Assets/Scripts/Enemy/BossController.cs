using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public partial class BossController : MonoBehaviour, IDamageable
{
    public static readonly List<BossController> Instances = new List<BossController>();

    [Header("Stats")]
    [SerializeField]
    private float maxHp = 1000f;

    [SerializeField]
    private float moveSpeed = 3f;

    [SerializeField]
    private float damage = 20f;

    [Header("Phase 2")]
    [Tooltip("HP 비율이 이 값 이하가 되면 Phase 2 진입")]
    [SerializeField]
    private float phase2Threshold = 0.4f;

    [Tooltip("Phase 2에서 패턴 간 쿨타임 배율 (1보다 작으면 빨라짐)")]
    [SerializeField]
    private float phase2CooldownMult = 0.3f;

    [Header("돌진 패턴")]
    [SerializeField]
    private float chargeSpeed = 12f;

    [SerializeField]
    private float chargeDuration = 0.5f;

    [SerializeField]
    private float chargeStunDuration = 0.5f;

    [Header("내려찍기 패턴")]
    [SerializeField]
    private float slamJumpForce = 15f;

    [SerializeField]
    private float slamFallSpeed = 20f;

    [SerializeField]
    private GameObject slamWarningPrefab;

    [Header("투사체 패턴")]
    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private int projectileCount = 3;

    [SerializeField]
    private float projectileSpeed = 8f;

    [SerializeField]
    private float projectileSpread = 30f;

    [Header("연속 베기 패턴")]
    [SerializeField]
    private int comboHitCount = 3;

    [SerializeField]
    private float comboInterval = 0.15f;

    [SerializeField]
    private float comboRange = 1.5f;

    [SerializeField]
    private Collider2D meleeHitbox;

    [Header("패턴 공통")]
    [SerializeField]
    private float patternCooldown = 0.6f;

    [SerializeField]
    private float tellDuration = 0.35f;

    [SerializeField]
    private float detectionRange = 12f;

    [Header("Drops")]
    [SerializeField]
    private GameObject goldDropPrefab;

    [SerializeField]
    private int goldDropMin = 20;

    [SerializeField]
    private int goldDropMax = 40;

    [Header("Knockback")]
    [SerializeField]
    private float knockbackForce = 3f;

    [SerializeField]
    private float knockbackDuration = 0.1f;

    [Header("Ground Check")]
    [SerializeField]
    private LayerMask groundLayer;

    [Header("Audio")]
    [SerializeField]
    private AudioClip deathSound;

    [SerializeField]
    private AudioClip slamSound;

    [SerializeField]
    private AudioClip comboSound;

    [SerializeField]
    private AudioClip projectileSound;

    [SerializeField]
    private AudioClip dashSound;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private float hp;
    private bool isDead;
    public bool IsDead => isDead;
    private bool isPhase2;
    private bool isActing;
    private float cooldownTimer;
    private bool attackFlip;

    private Transform player;
    private Color originalColor;

    [Header("UI")]
    [SerializeField]
    private string bossDisplayName = "BOSS";

    [SerializeField]
    private GameObject bossHealthBarUIPrefab;

    private BossHealthBarUI healthBarUI;
    private Animator animator;

    private ObjectPool<Projectile> projPool;

    [Header("Phase 2 - 바닥 가시")]
    [Tooltip("솟아오르는 가시 프리팹 (SpriteRenderer + Collider2D(IsTrigger) + BossSpike)")]
    [SerializeField]
    private GameObject spikePrefab;

    [Tooltip("가시/마법 낙하 예고 표식 프리팹 (이미지)")]
    [SerializeField]
    private GameObject warningPrefab;

    [Header("Phase 2 - 공중 마법")]
    [Tooltip("비우면 일반 투사체 프리팹을 재사용")]
    [SerializeField]
    private GameObject magicProjectilePrefab;

    [SerializeField]
    private float magicProjectileSpeed = 11f;

    private ObjectPool<Projectile> magicPool;
    private bool untargetable;
    private Vector3 preAirbornePos;

    public System.Action onDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        hp = maxHp;
        originalColor = sr.color;

        // BossHealthBarUI 인스턴스 확보: 씬에 없으면 프리팹/스크립트로 생성
        healthBarUI = BossHealthBarUI.Instance;
        if (healthBarUI == null)
        {
            if (bossHealthBarUIPrefab != null)
            {
                var go = Instantiate(bossHealthBarUIPrefab);
                healthBarUI = go.GetComponent<BossHealthBarUI>();
            }
            else
            {
                var go = new GameObject("BossHealthBarUI");
                healthBarUI = go.AddComponent<BossHealthBarUI>();
            }
        }
        healthBarUI.Show(bossDisplayName);
        healthBarUI.SetHealth(hp, maxHp);

        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
    }

    void OnEnable() => Instances.Add(this);

    void OnDisable() => Instances.Remove(this);

    void Start()
    {
        if (PlayerRef.Exists)
        {
            player = PlayerRef.Transform;
            Physics2D.IgnoreLayerCollision(gameObject.layer, PlayerRef.GameObject.layer, true);
        }
    }

    void Update()
    {
        if (isDead || player == null)
            return;

        // 보스 인트로 연출 중에는 행동 금지.
        if (BossIntro.IsPlaying)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        FlipToPlayer();

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange)
            return;

        if (isActing)
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = isPhase2 ? patternCooldown * phase2CooldownMult : patternCooldown;
            StartCoroutine(PickAndExecutePattern());
        }
        else
        {
            ChasePlayer();
        }
    }

    void FlipToPlayer()
    {
        if (player == null)
            return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.3f)
            return;
        bool flip = dx > 0f;
        sr.flipX = attackFlip ? !flip : flip;
    }

    void ChasePlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    // ── 패턴 선택 (실제 패턴 구현은 BossController.Patterns.cs) ──

    IEnumerator PickAndExecutePattern()
    {
        isActing = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (isPhase2)
        {
            // Phase 2: 가시 / 공중 마법 / 내려찍기
            switch (Random.Range(0, 3))
            {
                case 0:
                    yield return SpikeStormAttack();
                    break;
                case 1:
                    yield return AirMagicAttack();
                    break;
                case 2:
                    yield return SlamAttack();
                    break;
            }
        }
        else
        {
            float dist = Vector2.Distance(transform.position, player.position);

            // 근거리면 근접 패턴 우선, 원거리면 돌진/투사체
            int pattern;
            if (dist <= comboRange * 1.5f)
                pattern = Random.Range(0, 2); // 0: 연속베기, 1: 내려찍기
            else
                pattern = Random.Range(2, 4); // 2: 돌진, 3: 투사체

            switch (pattern)
            {
                case 0:
                    yield return ComboAttack();
                    break;
                case 1:
                    yield return SlamAttack();
                    break;
                case 2:
                    yield return ChargeAttack();
                    break;
                case 3:
                    yield return ProjectileAttack();
                    break;
            }
        }

        isActing = false;
        if (animator != null && !isDead)
            animator.Play("Idle", 0, 0f);
    }

    // ── 피격 ──

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || meleeHitbox == null || !meleeHitbox.enabled)
            return;
        if (!other.CompareTag("Player"))
            return;
        other.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (isDead)
            return;

        // 공중으로 이탈한 동안(가시/공중마법 패턴)은 피격 무시
        if (untargetable)
            return;

        amount *= MetaUpgrades.BossDamageMult;
        hp -= amount;
        DamagePopup.Spawn(transform.position + Vector3.up * 0.5f, amount);

        if (!isPhase2 && hp <= maxHp * phase2Threshold)
        {
            isPhase2 = true;
            StartCoroutine(Phase2Flash());
        }

        healthBarUI?.SetHealth(hp, maxHp);

        if (hp <= 0f)
        {
            if (meleeHitbox != null)
                meleeHitbox.enabled = false;
            Die();
            return;
        }

        StartCoroutine(HitFlash());

        if (!isActing && player != null)
            StartCoroutine(Knockback((transform.position - player.position).normalized));
    }

    IEnumerator HitFlash() => EnemyUtils.HitFlash(sr, originalColor, () => isDead);

    IEnumerator Phase2Flash()
    {
        for (int i = 0; i < 5; i++)
        {
            sr.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator Knockback(Vector2 dir)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            rb.linearVelocity = new Vector2(dir.x * knockbackForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ── 사망 ──

    void Die()
    {
        isDead = true;
        AudioManager.Instance?.PlaySFX(deathSound);
        StopAllCoroutines();
        if (animator != null)
            animator.enabled = false;
        sr.color = originalColor;
        healthBarUI?.SetHealth(0, maxHp);
        healthBarUI?.Hide();
        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (col != null)
            col.enabled = false;
        onDeath?.Invoke();
        onDeath = null;
        RunStats.Instance?.AddKill();
        SpawnDrops();
        StartCoroutine(DeathRoutine());
    }

    void SpawnDrops()
    {
        EnemyUtils.SpawnGoldDrops(
            goldDropPrefab,
            transform.position,
            groundLayer,
            5,
            goldDropMin,
            goldDropMax
        );
    }

    IEnumerator DeathRoutine()
    {
        yield return EnemyUtils.DeathBlink(sr);
        Destroy(gameObject);
    }
}
