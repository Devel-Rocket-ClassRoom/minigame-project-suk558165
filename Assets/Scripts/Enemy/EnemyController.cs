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

        float distToPlayer =
            player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        if (distToPlayer <= attackRange)
        {
            // 공격 범위 — 멈추고 공격
            Move(0f);
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                animator.SetTrigger(HashAttack);
                DealDamageToPlayer();
            }
        }
        else if (distToPlayer <= detectionRange)
        {
            // 감지 범위 — 플레이어 추격
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Move(dir);
        }
        else
        {
            // 감지 밖 — 순찰
            Patrol();
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

    void DealDamageToPlayer()
    {
        if (player == null)
            return;
        // 플레이어에 IDamageable이 있으면 데미지 전달
        var damageable = player.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage);
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

    void Die()
    {
        isDead = true;
        animator.SetBool(HashIsDead, true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
