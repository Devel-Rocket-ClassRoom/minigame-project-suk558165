using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;
    [Tooltip("원본 스프라이트가 향하는 각도 (오른쪽=0, 왼쪽=180)")]
    public float spriteAngleOffset;
    
    public ObjectPool<Projectile> Pool;

    private bool released;
    private float damage;
    private float knockbackForce;
    private GameObject shooter;
    private float lifesteal;
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
        float spinSpeed = 0f,
        float lifesteal = 0f
    )
    {
        this.damage = damage;
        released = false;
        ready = false;
        hitIds.Clear();
        CancelInvoke();
        this.knockbackForce = knockbackForce;
        this.shooter = shooter;
        this.lifesteal = lifesteal;
        this.pierceRemaining = pierce;
        this.spinSpeed = spinSpeed;
        rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteAngleOffset);
        Invoke(nameof(Activate), 0.05f);
        Invoke(nameof(ReleaseSelf), lifetime);
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
            var targetBody = other.attachedRigidbody;
            int targetLayer = targetBody != null ? targetBody.gameObject.layer : other.gameObject.layer;
            bool targetIsEnemy = targetLayer == enemyLayer;

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

            damageable.TakeDamage(damage, shooter);

            // 흡혈: 플레이어가 쏜 투사체가 적을 맞히면 가한 데미지 비율만큼 회복
            if (lifesteal > 0f && shooter != null)
                shooter.GetComponent<PlayerHealth>()?.Heal(damage * lifesteal);

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
                ReleaseSelf();
            else
                pierceRemaining--;
        }
    }

    void ReleaseSelf()
    {
        if (released)
            return;

        released = true;
        CancelInvoke();
        if (Pool != null)
            Pool.Release(this);
        else
            Destroy(gameObject);
    }
}
