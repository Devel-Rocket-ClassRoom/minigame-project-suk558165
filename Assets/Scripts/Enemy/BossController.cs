using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    private CancellationTokenSource _cts = new();

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

    CancellationToken RefreshToken()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

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

    void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

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

        float dist = Vector2.Distance(transform.position, player.position);

        if (isActing)
            return;

        FlipToPlayer();

        // 인식 범위 밖이면 패턴 없이 추격만, 안에서는 정상 패턴.
        // 단 추격 속도는 근접 범위에 들어오기 전까진 항상 부스트 —
        // detectionRange 경계에서 속도가 급감해 무한 카이팅되는 함정 방지.
        bool needBoost = dist > comboRange * 1.5f;

        if (dist > detectionRange)
        {
            ChasePlayer(boost: true);
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = isPhase2 ? patternCooldown * phase2CooldownMult : patternCooldown;
            PickAndExecutePattern(_cts.Token).Forget();
        }
        else
        {
            ChasePlayer(boost: needBoost);
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

    void ChasePlayer(bool boost = false)
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        // 인식 범위 밖에서 호출될 땐 부스트 — 플레이어가 도망쳐도 따라잡을 수 있는 속도.
        float speed = boost ? moveSpeed * 2.5f : moveSpeed;
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    // ── 패턴 선택 (실제 패턴 구현은 BossController.Patterns.cs) ──

    async UniTaskVoid PickAndExecutePattern(CancellationToken token)
    {
        isActing = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (isPhase2)
        {
            // Phase 2: 가시 / 공중 마법 / 내려찍기
            switch (Random.Range(0, 3))
            {
                case 0:
                    await SpikeStormAttack(token);
                    break;
                case 1:
                    await AirMagicAttack(token);
                    break;
                case 2:
                    await SlamAttack(token);
                    break;
            }
        }
        else
        {
            float dist = Vector2.Distance(transform.position, player.position);

            // 근거리면 근접 패턴 우선, 원거리면 돌진/투사체.
            // 매우 먼 거리(>6m)에선 무조건 돌진으로 거리부터 좁힘 — 플레이어가 카이팅 못 하게.
            int pattern;
            if (dist <= comboRange * 1.5f)
                pattern = Random.Range(0, 2); // 0: 연속베기, 1: 내려찍기
            else if (dist > 6f)
                pattern = 2; // 무조건 돌진
            else
                pattern = Random.Range(2, 4); // 2: 돌진, 3: 투사체 (50/50)

            switch (pattern)
            {
                case 0:
                    await ComboAttack(token);
                    break;
                case 1:
                    await SlamAttack(token);
                    break;
                case 2:
                    await ChargeAttack(token);
                    break;
                case 3:
                    await ProjectileAttack(token);
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
            Phase2Flash(_cts.Token).Forget();
        }

        healthBarUI?.SetHealth(hp, maxHp);

        if (hp <= 0f)
        {
            if (meleeHitbox != null)
                meleeHitbox.enabled = false;
            Die();
            return;
        }

        HitFlash().Forget();

        // 근접 범위에서만 knockback — 원거리 피격 시엔 추격을 끊지 않음.
        // (멀리서 화살 연사로 knockback이 계속 발생해 추격이 끊기는 문제 방지)
        if (!isActing && player != null
            && Vector2.Distance(transform.position, player.position) <= comboRange * 2f)
            Knockback((transform.position - player.position).normalized, _cts.Token).Forget();
    }

    UniTask HitFlash() => EnemyUtils.HitFlash(sr, originalColor, () => isDead);

    async UniTaskVoid Phase2Flash(CancellationToken token)
    {
        for (int i = 0; i < 5; i++)
        {
            sr.color = new Color(1f, 0.3f, 0.3f);
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), cancellationToken: token);
            sr.color = originalColor;
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), cancellationToken: token);
        }
    }

    async UniTaskVoid Knockback(Vector2 dir, CancellationToken token)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            rb.linearVelocity = new Vector2(dir.x * knockbackForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ── 사망 ──

    void Die()
    {
        isDead = true;
        AudioManager.Instance?.PlaySFX(deathSound);
        var token = RefreshToken();
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
        RunStats.Instance?.AddBossKill();
        SpawnDrops();
        DeathRoutine(token).Forget();
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

    async UniTaskVoid DeathRoutine(CancellationToken token)
    {
        await EnemyUtils.DeathBlink(sr);
        Destroy(gameObject);
    }
}
