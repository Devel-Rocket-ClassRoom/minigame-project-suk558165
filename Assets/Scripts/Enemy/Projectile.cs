using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;

    private float damage;
    private float knockbackForce;
    private GameObject shooter;
    private bool ready;
    private int pierceRemaining;
    private float spinSpeed;
    private System.Collections.Generic.HashSet<int> hitIds = new();
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 모든 콜라이더를 트리거로 설정 (이전 RequireComponent로 남은 콜라이더 대응)
        foreach (var col in GetComponents<Collider2D>())
            col.isTrigger = true;
    }

    public void Init(
        Vector2 direction,
        float speed,
        float damage,
        GameObject shooter = null,
        float knockbackForce = 8f,
        int pierce = 0,
        float spinSpeed = 0f
    )
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.shooter = shooter;
        this.pierceRemaining = pierce;
        this.spinSpeed = spinSpeed;
        rb.linearVelocity = direction.normalized * speed;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = direction.x > 0f;
        Invoke(nameof(Activate), 0.05f);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (spinSpeed != 0f)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void Activate() => ready = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ready)
            return;

        if (other.isTrigger)
            return;

        if (shooter != null && other.transform.IsChildOf(shooter.transform))
            return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int shooterLayer = shooter != null ? shooter.layer : -1;

        if (shooter != null)
        {
            bool shooterIsEnemy = shooterLayer == enemyLayer;
            // 히트 대상의 루트 오브젝트 레이어로 팀 판별 (자식 콜라이더 레이어 불일치 대응)
            int rootLayer = other.transform.root.gameObject.layer;
            bool targetIsEnemy = rootLayer == enemyLayer;

            if (shooterIsEnemy && targetIsEnemy)
                return; // 적 → 적 무시
            if (!shooterIsEnemy && !targetIsEnemy)
                return; // 플레이어 → 플레이어 무시
        }

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (!hitIds.Add(other.GetInstanceID()))
                return;

            damageable.TakeDamage(damage);

            if (other.CompareTag("Player"))
            {
                var playerCtrl = other.GetComponentInParent<PlayerController>();
                if (playerCtrl != null)
                {
                    Vector2 dir = rb.linearVelocity.normalized;
                    playerCtrl.Knockback(new Vector2(dir.x, 0.3f).normalized * knockbackForce);
                }
            }

            if (pierceRemaining <= 0)
                Destroy(gameObject);
            else
                pierceRemaining--;
        }
    }
}
