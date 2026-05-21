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

    [Header("Edge Detection")]
    public float edgeCheckDepth = 1.5f;

    [Tooltip("공격 애니메이션에서 실제 타격 판정이 나오는 타이밍 (초).")]
    public float attackDamageDelay = 1f;

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
        {
            player = playerGO.transform;
            Physics2D.IgnoreLayerCollision(gameObject.layer, playerGO.layer, true);
        }
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
        if (dist <= detectionRange)
        {
            if (dist > attackRange)
            {
                float dir = player.position.x > transform.position.x ? 1f : -1f;
                Move(IsEdgeAhead(dir) ? 0f : dir);
            }
            else
            {
                Move(0f);
            }

            if (attackTimer <= 0f && dist <= attackRange)
            {
                attackTimer = attackCooldown;
                animator.SetTrigger(HashAttack);
                StartCoroutine(DealDamageAfterDelay(attackDamageDelay));
            }
        }
        else
        {
            Patrol();
        }
    }

    void UpdateRanged(float dist)
    {
        if (dist <= detectionRange)
        {
            if (dist < safeDistance)
            {
                float dir = transform.position.x > player.position.x ? 1f : -1f;
                Move(IsEdgeAhead(dir) ? 0f : dir);
            }
            else
            {
                Move(0f);
            }

            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                animator.SetTrigger(HashAttack);
                StartCoroutine(ShootAfterDelay(attackDamageDelay));
            }
        }
        else
        {
            Patrol();
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || player == null)
            return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Init(dir, projectileSpeed, damage, gameObject);
    }

    void Patrol()
    {
        float distFromOrigin = transform.position.x - patrolOrigin.x;

        if (distFromOrigin >= patrolDistance)
            patrolDir = -1;
        else if (distFromOrigin <= -patrolDistance)
            patrolDir = 1;

        if (IsEdgeAhead(patrolDir))
            patrolDir = -patrolDir;

        Move(patrolDir);
    }

    bool IsEdgeAhead(float dir)
    {
        if (dir == 0f)
            return false;
        var col = GetComponent<Collider2D>();
        float xOffset = (col != null ? col.bounds.extents.x : 0.3f) + 0.2f;
        Vector2 origin = new Vector2(transform.position.x + dir * xOffset, transform.position.y);
        return Physics2D.Raycast(origin, Vector2.down, edgeCheckDepth, groundLayer).collider
            == null;
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

    IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isDead || player == null)
            yield break;
        if (Vector2.Distance(transform.position, player.position) <= attackRange * 1.5f)
            player.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    IEnumerator ShootAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isDead)
            ShootProjectile();
    }

    public void DealDamageToPlayer()
    {
        player?.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        hp -= amount;
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
        animator.SetBool(HashIsDead, true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        onDeath?.Invoke();
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (isRanged)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }

        if (meleeHitbox != null && meleeHitbox.enabled)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            var b = meleeHitbox.bounds;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
