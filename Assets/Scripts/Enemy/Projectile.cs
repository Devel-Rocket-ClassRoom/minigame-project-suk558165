using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;

    private float damage;
    private GameObject shooter;
    private bool ready;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Init(Vector2 direction, float speed, float damage, GameObject shooter = null)
    {
        this.damage = damage;
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

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
