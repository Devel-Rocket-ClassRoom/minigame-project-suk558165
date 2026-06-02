using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class MiniBossController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField]
    private float maxHp = 250f;

    [SerializeField]
    private float moveSpeed = 3.5f;

    [SerializeField]
    private float damage = 15f;

    [Header("지면 파동")]
    [SerializeField]
    private GameObject wavePrefab;

    [SerializeField]
    private float waveSpeed = 6f;

    [Header("도약 베기")]
    [SerializeField]
    private float leapJumpForce = 18f;

    [SerializeField]
    private float leapFallSpeed = 25f;

    [SerializeField]
    private float leapRadius = 2f;

    [SerializeField]
    private Collider2D meleeHitbox;

    [Header("연속 돌진")]
    [SerializeField]
    private int dashCount = 3;

    [SerializeField]
    private float dashSpeed = 14f;

    [SerializeField]
    private float dashDuration = 0.25f;

    [SerializeField]
    private float dashInterval = 0.3f;

    [Header("패턴 공통")]
    [SerializeField]
    private float patternCooldown = 2.5f;

    [SerializeField]
    private float tellDuration = 0.5f;

    [SerializeField]
    private float detectionRange = 12f;

    [Header("Drops")]
    [SerializeField]
    private GameObject goldDropPrefab;

    [SerializeField]
    private int goldDropMin = 10;

    [SerializeField]
    private int goldDropMax = 20;

    [Header("Ground Check")]
    [SerializeField]
    private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Color originalColor;

    private float hp;
    private bool isDead;
    private bool isActing;
    private float cooldownTimer;
    private bool dashHitThisSegment;

    private Transform player;
    public System.Action onDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        hp = maxHp;
        originalColor = sr.color;

        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
    }

    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            Physics2D.IgnoreLayerCollision(gameObject.layer, playerGO.layer, true);
        }
    }

    void Update()
    {
        if (isDead || player == null)
            return;
        if (Vector2.Distance(transform.position, player.position) > detectionRange)
            return;

        FlipToPlayer();

        if (isActing)
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = patternCooldown;
            StartCoroutine(ExecutePattern());
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

    IEnumerator ExecutePattern()
    {
        isActing = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        int pattern = Random.Range(0, 3);
        switch (pattern)
        {
            case 0:
                yield return GroundWave();
                break;
            case 1:
                yield return LeapSlash();
                break;
            case 2:
                yield return MultiDash();
                break;
        }

        isActing = false;
    }

    // ── 텔 연출 ──────────────────────────────────────

    IEnumerator TellFlash(Color color)
    {
        float elapsed = 0f;
        while (elapsed < tellDuration)
        {
            sr.color = Color.Lerp(originalColor, color, Mathf.PingPong(elapsed * 10f, 1f));
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
            transform.position = origin + new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = origin;
    }

    // ── 패턴 1: 지면 파동 ─────────────────────────────
    // 바닥을 내리쳐 좌우로 파동이 퍼져나감

    IEnumerator GroundWave()
    {
        yield return TellShake();

        rb.linearVelocity = Vector2.zero;

        if (wavePrefab != null)
        {
            Vector3 origin = transform.position;

            var left = Instantiate(wavePrefab, origin, Quaternion.identity);
            left.GetComponent<Projectile>()?.Init(Vector2.left, waveSpeed, damage, gameObject);

            var right = Instantiate(wavePrefab, origin, Quaternion.identity);
            right.GetComponent<Projectile>()?.Init(Vector2.right, waveSpeed, damage, gameObject);
        }

        yield return new WaitForSeconds(0.6f);
    }

    // ── 패턴 2: 도약 베기 ─────────────────────────────
    // 점프 후 플레이어 위치로 낙하, 착지 충격파

    IEnumerator LeapSlash()
    {
        yield return TellFlash(Color.yellow);

        rb.linearVelocity = new Vector2(0f, leapJumpForce);
        yield return new WaitForSeconds(0.35f);

        // 플레이어 X로 이동 후 급낙하
        if (player != null)
        {
            Vector3 p = transform.position;
            p.x = player.position.x;
            transform.position = p;
        }

        while (!IsGrounded())
        {
            rb.linearVelocity = new Vector2(0f, -leapFallSpeed);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // 착지 충격 — 멜레이 히트박스 순간 활성
        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            meleeHitbox.enabled = false;
        }
        else
        {
            DealAreaDamage(transform.position, leapRadius);
        }

        yield return new WaitForSeconds(0.4f);
    }

    // ── 패턴 3: 연속 돌진 ─────────────────────────────
    // 짧은 대시를 dashCount 회 반복, 마지막에 짧은 스턴

    IEnumerator MultiDash()
    {
        yield return TellFlash(Color.cyan);

        for (int i = 0; i < dashCount; i++)
        {
            if (player == null)
                break;

            FlipToPlayer();
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            dashHitThisSegment = false;

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                rb.linearVelocity = new Vector2(dir * dashSpeed, rb.linearVelocity.y);

                if (!dashHitThisSegment)
                    DealAreaDamage(transform.position, 0.9f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (i < dashCount - 1)
                yield return new WaitForSeconds(dashInterval);
        }

        // 스턴
        rb.linearVelocity = Vector2.zero;
        sr.color = Color.gray;
        yield return new WaitForSeconds(0.6f);
        sr.color = originalColor;
    }

    // ── 유틸 ──────────────────────────────────────────

    void DealAreaDamage(Vector3 center, float radius)
    {
        if (player == null)
            return;
        if (Vector2.Distance(center, player.position) > radius)
            return;

        player.GetComponent<IDamageable>()?.TakeDamage(damage);
        dashHitThisSegment = true;

        var ctrl = player.GetComponent<PlayerController>();
        if (ctrl != null)
        {
            Vector2 knockDir = ((Vector2)player.position - (Vector2)center).normalized;
            ctrl.Knockback(new Vector2(knockDir.x, 0.3f).normalized * 7f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || meleeHitbox == null || !meleeHitbox.enabled)
            return;
        if (!other.CompareTag("Player"))
            return;
        other.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
    }

    bool IsGrounded()
    {
        float footY = col != null ? col.bounds.min.y : transform.position.y;
        return Physics2D
                .Raycast(
                    new Vector2(transform.position.x, footY + 0.05f),
                    Vector2.down,
                    0.2f,
                    groundLayer
                )
                .collider != null;
    }

    // ── 피격 / 사망 ────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;
        hp -= amount;

        if (hp <= 0f)
        {
            Die();
            return;
        }

        StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!isDead)
            sr.color = originalColor;
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        sr.color = originalColor;
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
        for (int i = 0; i < 3; i++)
        {
            var gold = Instantiate(goldDropPrefab, pos, Quaternion.identity);
            var worldGold = gold.GetComponent<WorldGold>();
            if (worldGold != null)
            {
                worldGold.amount = Random.Range(goldDropMin, goldDropMax + 1);
                float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
                worldGold.Launch(
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(3f, 6f),
                    floorY
                );
            }
        }
    }

    IEnumerator DeathRoutine()
    {
        for (int i = 0; i < 8; i++)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.15f);
        }
        Destroy(gameObject);
    }
}
