using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossController : MonoBehaviour, IDamageable
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
    private float phase2Threshold = 0.5f;

    [Tooltip("Phase 2에서 패턴 간 쿨타임 배율 (1보다 작으면 빨라짐)")]
    [SerializeField]
    private float phase2CooldownMult = 0.5f;

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

    // ── 패턴 선택 ──

    IEnumerator PickAndExecutePattern()
    {
        isActing = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

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

        isActing = false;
        if (animator != null && !isDead)
            animator.Play("Idle", 0, 0f);
    }

    // ── 텔 (예고 연출) ──

    IEnumerator TellFlash(Color color) =>
        EnemyUtils.TellFlash(sr, color, originalColor, tellDuration);

    IEnumerator TellShake() => EnemyUtils.TellShake(transform, tellDuration);

    // ── 패턴: 돌진 공격 (돌진 후 베기) ──

    IEnumerator ChargeAttack()
    {
        yield return TellFlash(Color.red);

        attackFlip = true;
        FlipToPlayer();
        AudioManager.Instance?.PlaySFX(dashSound);
        if (animator != null)
            animator.Play("Dash", 0, 0f);

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        float elapsed = 0f;

        // 돌진: 플레이어 근처까지 이동 (데미지 없음)
        while (elapsed < chargeDuration)
        {
            transform.position += new Vector3(dir * chargeSpeed * Time.deltaTime, 0f, 0f);

            if (Vector2.Distance(transform.position, player.position) <= comboRange)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 도착 후 베기
        rb.linearVelocity = Vector2.zero;
        FlipToPlayer();
        if (animator != null)
            animator.Play("Dash", 0, 0f);

        yield return new WaitForSeconds(0.2f);
        DealAreaDamage(transform.position, comboRange);
        yield return new WaitForSeconds(0.15f);

        attackFlip = false;

        // 스턴 (반격 타이밍)
        sr.color = Color.gray;
        yield return new WaitForSeconds(chargeStunDuration);
        sr.color = originalColor;
    }

    // ── 패턴: 내려찍기 ──

    IEnumerator SlamAttack()
    {
        yield return TellShake();

        if (animator != null)
            animator.Play("Slam", 0, 0f);

        // 점프
        rb.linearVelocity = new Vector2(0f, slamJumpForce);

        // 점프 후 실제로 지면을 벗어날 때까지 대기 (바로 IsGrounded 체크하면 아직 지면 접촉 판정)
        float liftWait = 0f;
        while (IsGrounded() && liftWait < 0.3f)
        {
            liftWait += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // 경고 표시 (바닥 전체)
        Vector3 targetPos = player.position;
        GameObject warning = null;
        if (slamWarningPrefab != null)
        {
            warning = Instantiate(slamWarningPrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = new Vector3(100f, 0.3f, 1f);
        }

        yield return new WaitForSeconds(0.15f);

        // 급강하 — Rigidbody 통해 X 이동 (transform.position 직접 수정은 물리 디싱크 유발)
        rb.MovePosition(new Vector2(targetPos.x, rb.position.y));
        rb.linearVelocity = new Vector2(0f, -slamFallSpeed);

        float fallTimeout = 3f;
        float fallElapsed = 0f;
        while (!IsGrounded() && fallElapsed < fallTimeout)
        {
            rb.linearVelocity = new Vector2(0f, -slamFallSpeed);
            fallElapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        if (warning != null)
            Destroy(warning);

        // 착지 데미지
        AudioManager.Instance?.PlaySFX(slamSound);
        SlamGroundDamage();

        yield return new WaitForSeconds(0.25f);
    }

    // ── 패턴: 투사체 ──

    IEnumerator ProjectileAttack()
    {
        yield return TellFlash(new Color(1f, 0.5f, 0f));

        FlipToPlayer();
        if (animator != null)
            animator.Play("Charge", 0, 0f);

        if (projectilePrefab == null || player == null)
            yield break;

        AudioManager.Instance?.PlaySFX(projectileSound);
        Vector2 baseDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projectileCount; i++)
        {
            float offset = 0f;
            if (projectileCount > 1)
                offset = Mathf.Lerp(
                    -projectileSpread / 2f,
                    projectileSpread / 2f,
                    (float)i / (projectileCount - 1)
                );

            float angle = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projComp = proj.GetComponent<Projectile>();
            if (projComp != null)
                projComp.Init(dir, projectileSpeed, damage, gameObject);
        }

        yield return new WaitForSeconds(0.25f);
    }

    // ── 패턴: 연속 베기 ──

    IEnumerator ComboAttack()
    {
        yield return TellShake();

        attackFlip = true;
        FlipToPlayer();
        AudioManager.Instance?.PlaySFX(comboSound);
        if (animator != null)
            animator.Play("Combo", 0, 0f);

        for (int i = 0; i < comboHitCount; i++)
        {
            FlipToPlayer();
            DealAreaDamage(transform.position, comboRange);

            if (i < comboHitCount - 1)
                yield return new WaitForSeconds(comboInterval);
        }

        attackFlip = false;
        yield return new WaitForSeconds(0.15f);
    }

    // ── 범위 데미지 ──

    void DealAreaDamage(Vector3 center, float radius)
    {
        if (player == null)
            return;
        if (Vector2.Distance(center, player.position) <= radius)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                Vector2 knockDir = ((Vector2)player.position - (Vector2)center).normalized;
                playerCtrl.Knockback(knockDir * 8f);
            }
        }
    }

    void SlamGroundDamage()
    {
        if (player == null)
            return;

        float footY = col != null ? col.bounds.min.y : transform.position.y;
        float playerY = player.position.y;

        if (Mathf.Abs(playerY - footY) <= 2f)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
                playerCtrl.Knockback(Vector2.up * 8f);
        }
    }

    bool IsGrounded() => EnemyUtils.IsGrounded(col, transform, groundLayer);

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
