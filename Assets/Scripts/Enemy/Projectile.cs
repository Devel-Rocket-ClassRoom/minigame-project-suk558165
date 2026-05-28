using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;

    private float damage;
    private float knockbackForce;
    private GameObject shooter;
    private bool ready;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Init(
        Vector2 direction,
        float speed,
        float damage,
        GameObject shooter = null,
        float knockbackForce = 8f
    )
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.shooter = shooter;
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        Invoke(nameof(Activate), 0.05f);
        Destroy(gameObject, lifetime);
    }

    void Activate() => ready = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ready)
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
            damageable.TakeDamage(damage);

            if (other.CompareTag("Player"))
            {
                var playerCtrl = other.GetComponentInParent<PlayerController>();
                if (playerCtrl != null)
                {
                    Vector2 dir = GetComponent<Rigidbody2D>().linearVelocity.normalized;
                    playerCtrl.Knockback(new Vector2(dir.x, 0.3f).normalized * knockbackForce);
                }
            }

            Destroy(gameObject);
        }
    }
}
