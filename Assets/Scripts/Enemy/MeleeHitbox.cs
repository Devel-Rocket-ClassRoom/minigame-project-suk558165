using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    public float damage = 10f;
    public float knockbackForce = 10f;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void Activate(float duration)
    {
        col.enabled = true;
        Invoke(nameof(Deactivate), duration);
    }

    void Deactivate() => col.enabled = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        other.GetComponentInParent<IDamageable>()?.TakeDamage(damage);

        var playerCtrl = other.GetComponentInParent<PlayerController>();
        if (playerCtrl != null)
        {
            Vector2 dir = (other.transform.root.position - transform.position).normalized;
            playerCtrl.Knockback(new Vector2(dir.x, 0.4f).normalized * knockbackForce);
        }
    }

    void OnDrawGizmos()
    {
        var c = col != null ? col : GetComponent<Collider2D>();
        if (c == null)
            return;

        bool active = c.enabled;
        Gizmos.color = active ? new Color(1f, 0.1f, 0.1f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.3f);
        var b = c.bounds;
        Gizmos.DrawWireCube(b.center, b.size);
        if (active)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
