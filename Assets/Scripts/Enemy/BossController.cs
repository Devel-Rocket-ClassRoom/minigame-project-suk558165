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
    private float maxHp = 500f;

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
    private float phase2CooldownMult = 0.7f;

    [Header("돌진 패턴")]
    [SerializeField]
    private float chargeSpeed = 12f;

    [SerializeField]
    private float chargeDuration = 0.5f;

    [SerializeField]
    private float chargeStunDuration = 1f;

    [Header("내려찍기 패턴")]
    [SerializeField]
    private float slamJumpForce = 15f;

    [SerializeField]
    private float slamFallSpeed = 20f;

    [SerializeField]
    private float slamRadius = 3f;

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
    private float comboInterval = 0.3f;

    [SerializeField]
    private float comboRange = 1.5f;

    [SerializeField]
    private Collider2D meleeHitbox;

    [Header("패턴 공통")]
    [SerializeField]
    private float patternCooldown = 2f;

    [SerializeField]
    private float tellDuration = 0.6f;

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

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private float hp;
    private bool isDead;
    public bool IsDead => isDead;
    private bool isPhase2;
    private bool isActing;
    private float cooldownTimer;

    private Transform player;
    private Color originalColor;
    private EnemyHealthBar healthBar;

    public System.Action onDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        hp = maxHp;
        originalColor = sr.color;
        healthBar = gameObject.AddComponent<EnemyHealthBar>();
        healthBar.Init(new Vector3(0f, -0.6f, 0f), 2f);

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

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange)
            return;

        FlipToPlayer();

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
        sr.flipX = player.position.x < transform.position.x;
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
    }

    // ── 텔 (예고 연출) ──

    IEnumerator TellFlash(Color color)
    {
        float elapsed = 0f;
        while (elapsed < tellDuration)
        {
            float t = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(originalColor, color, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        sr.color = originalColor;
    }

    IEnumerator TellShake()
    {
        Vector3 origin = transform.position;
        float elapsed = 0f;
        while (elapsed < tellDuration)
        {
            float offsetX = Random.Range(-0.05f, 0.05f);
            transform.position = origin + new Vector3(offsetX, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = origin;
    }

    // ── 패턴: 돌진 ──

    IEnumerator ChargeAttack()
    {
        yield return TellFlash(Color.red);

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        float elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            rb.linearVelocity = new Vector2(dir * chargeSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 스턴 (반격 타이밍)
        rb.linearVelocity = Vector2.zero;
        sr.color = Color.gray;
        yield return new WaitForSeconds(chargeStunDuration);
        sr.color = originalColor;
    }

    // ── 패턴: 내려찍기 ──

    IEnumerator SlamAttack()
    {
        yield return TellShake();

        // 점프
        rb.linearVelocity = new Vector2(0f, slamJumpForce);
        yield return new WaitForSeconds(0.4f);

        // 경고 표시
        Vector3 targetPos = player.position;
        GameObject warning = null;
        if (slamWarningPrefab != null)
        {
            warning = Instantiate(slamWarningPrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = new Vector3(slamRadius * 2f, 0.3f, 1f);
        }

        yield return new WaitForSeconds(0.3f);

        // 급강하
        transform.position = new Vector3(targetPos.x, transform.position.y, 0f);
        while (!IsGrounded())
        {
            rb.linearVelocity = new Vector2(0f, -slamFallSpeed);
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        if (warning != null)
            Destroy(warning);

        // 착지 데미지
        DealAreaDamage(transform.position, slamRadius);

        yield return new WaitForSeconds(0.5f);
    }

    // ── 패턴: 투사체 ──

    IEnumerator ProjectileAttack()
    {
        yield return TellFlash(new Color(1f, 0.5f, 0f));

        if (projectilePrefab == null || player == null)
            yield break;

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

        yield return new WaitForSeconds(0.5f);
    }

    // ── 패턴: 연속 베기 ──

    IEnumerator ComboAttack()
    {
        yield return TellShake();

        for (int i = 0; i < comboHitCount; i++)
        {
            FlipToPlayer();

            if (meleeHitbox != null)
            {
                meleeHitbox.enabled = true;
                yield return new WaitForSeconds(0.1f);
                meleeHitbox.enabled = false;
            }
            else
            {
                DealAreaDamage(transform.position, comboRange);
            }

            if (i < comboHitCount - 1)
                yield return new WaitForSeconds(comboInterval);
        }

        yield return new WaitForSeconds(0.3f);
    }

    // ── 범위 데미지 ──

    void DealAreaDamage(Vector3 center, float radius)
    {
        if (player == null)
            return;
        if (Vector2.Distance(center, player.position) <= radius)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(damage);
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                Vector2 knockDir = ((Vector2)player.position - (Vector2)center).normalized;
                playerCtrl.Knockback(knockDir * 8f);
            }
        }
    }

    bool IsGrounded()
    {
        float footY = col != null ? col.bounds.min.y : transform.position.y;
        Vector2 origin = new Vector2(transform.position.x, footY + 0.05f);
        return Physics2D.Raycast(origin, Vector2.down, 0.2f, groundLayer).collider != null;
    }

    // ── 피격 ──

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || meleeHitbox == null || !meleeHitbox.enabled)
            return;
        if (!other.CompareTag("Player"))
            return;
        other.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        hp -= amount;

        if (!isPhase2 && hp <= maxHp * phase2Threshold)
        {
            isPhase2 = true;
            StartCoroutine(Phase2Flash());
        }

        healthBar?.SetHealth(hp, maxHp);

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

    IEnumerator HitFlash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!isDead)
            sr.color = originalColor;
    }

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
        StopAllCoroutines();
        sr.color = originalColor;
        healthBar?.SetHealth(0, maxHp);
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
        if (goldDropPrefab == null)
            return;

        Vector3 pos = transform.position + Vector3.up * 0.3f;
        float floorY = transform.position.y;
        var groundHit = Physics2D.Raycast(transform.position, Vector2.down, 20f, groundLayer);
        if (groundHit.collider != null)
            floorY = groundHit.point.y;

        for (int i = 0; i < 5; i++)
        {
            var gold = Instantiate(goldDropPrefab, pos, Quaternion.identity);
            var worldGold = gold.GetComponent<WorldGold>();
            if (worldGold != null)
            {
                worldGold.amount = Random.Range(goldDropMin, goldDropMax + 1);
                float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
                float force = Random.Range(3f, 6f);
                worldGold.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * force, floorY);
            }
        }
    }

    IEnumerator DeathRoutine()
    {
        // 깜빡이며 사라짐
        for (int i = 0; i < 8; i++)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.15f);
        }
        Destroy(gameObject);
    }
}
