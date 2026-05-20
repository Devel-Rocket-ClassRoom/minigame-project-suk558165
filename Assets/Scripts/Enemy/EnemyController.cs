using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHp = 50f;
    public float moveSpeed = 2f;
    public float damage = 10f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.2f;
    public LayerMask groundLayer;

    [Header("Patrol")]
    public float patrolDistance = 4f;

    [Header("Ranged")]
    public bool isRanged = false;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 8f;
    public float safeDistance = 3f;

    [Header("Melee")]
    public Collider2D meleeHitbox;
    public float meleeHitDuration = 0.15f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private float hp;
    private bool isDead;
    private float attackTimer;

    private Transform player;
    private Vector2 patrolOrigin;
    private int patrolDir = 1;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashIsDead = Animator.StringToHash("IsDead");
    private static readonly int HashIsHit = Animator.StringToHash("IsHit");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        hp = maxHp;
        patrolOrigin = transform.position;

        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
    }

    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;
    }

    void Update()
    {
        if (isDead)
            return;

        attackTimer -= Time.deltaTime;

        float dist =
            player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        if (isRanged)
            UpdateRanged(dist);
        else
            UpdateMelee(dist);
    }

    void UpdateMelee(float dist)
    {
        if (dist <= attackRange)
        {
            Move(0f);
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                animator.SetTrigger(HashAttack);
                StartCoroutine(ActivateMeleeHitbox());
            }
        }
        else if (dist <= detectionRange)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Move(dir);
        }
        else
        {
            Patrol();
        }
    }

    void UpdateRanged(float dist)
    {
        if (dist <= attackRange)
        {
            // 너무 가까우면 뒤로 물러남
            if (dist < safeDistance)
            {
                float dir = transform.position.x > player.position.x ? 1f : -1f;
                Move(dir);
            }
            else
            {
                Move(0f);
            }

            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                animator.SetTrigger(HashAttack);
                ShootProjectile();
            }
        }
        else if (dist <= detectionRange)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Move(dir);
        }
        else
        {
            Patrol();
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || player == null)
        {
            Debug.LogWarning($"[{name}] projectilePrefab 미할당 — Inspector에서 설정 필요");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.Init(dir, projectileSpeed, damage, gameObject);
            Debug.Log(
                $"[{name}] 투사체 발사 → 방향 {dir}, 속도 {projectileSpeed}, 데미지 {damage}"
            );
        }
    }

    void Patrol()
    {
        float distFromOrigin = transform.position.x - patrolOrigin.x;

        if (distFromOrigin >= patrolDistance)
            patrolDir = -1;
        else if (distFromOrigin <= -patrolDistance)
            patrolDir = 1;

        Move(patrolDir);
    }

    void Move(float dir)
    {
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        animator.SetFloat(HashSpeed, Mathf.Abs(dir));

        if (dir > 0f)
            sr.flipX = false;
        else if (dir < 0f)
            sr.flipX = true;
    }

    IEnumerator ActivateMeleeHitbox()
    {
        yield return new WaitForSeconds(meleeHitDuration * 0.5f);

        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = true;
            var results = new Collider2D[4];
            int count = Physics2D.OverlapCollider(
                meleeHitbox,
                new ContactFilter2D().NoFilter(),
                results
            );
            for (int i = 0; i < count; i++)
            {
                if (results[i].CompareTag("Player"))
                {
                    results[i].GetComponent<IDamageable>()?.TakeDamage(damage);
                    Debug.Log($"[{name}] 근접 공격 명중 — 데미지 {damage}");
                    break;
                }
            }
            meleeHitbox.enabled = false;
        }
        else
        {
            if (
                player != null
                && Vector2.Distance(transform.position, player.position) <= attackRange
            )
            {
                player.GetComponent<IDamageable>()?.TakeDamage(damage);
                Debug.Log($"[{name}] 근접 공격 명중(거리 판정) — 데미지 {damage}");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (meleeHitbox == null || !meleeHitbox.enabled)
            return;
        if (!other.CompareTag("Player"))
            return;
        other.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    public void DealDamageToPlayer()
    {
        if (player == null)
            return;
        player.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        float before = hp;
        hp -= amount;
        Debug.Log($"[{name}] 피격 -{amount}  HP: {before:F0} → {Mathf.Max(hp, 0):F0} / {maxHp:F0}");

        animator.SetTrigger(HashIsHit);

        if (hp <= 0f)
            Die();
    }

    public System.Action onDeath;

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
        Debug.Log($"[{name}] 사망");
        animator.SetBool(HashIsDead, true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        onDeath?.Invoke();
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (isRanged)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }
    }
}
