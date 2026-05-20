using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    public float damage = 10f;

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
    }
}
