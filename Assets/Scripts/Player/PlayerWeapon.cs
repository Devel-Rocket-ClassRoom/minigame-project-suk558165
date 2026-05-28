using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Hit Detection")]
    public Transform attackPoint;
    public float hitRadius = 0.7f;
    public float damage = 20f;
    public LayerMask enemyLayer;

    private SpriteRenderer weaponSr;
    private Transform playerRoot;
    private Transform visuals;

    void Awake()
    {
        weaponSr = GetComponent<SpriteRenderer>();
        playerRoot = transform.root;
        visuals = playerRoot.Find("Visuals");

        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");
    }

    public void ApplyWeaponData(WeaponData data)
    {
        damage = data.damage;
        if (weaponSr != null)
            weaponSr.sprite = data.sprite;
    }

    public void OnHitFrame()
    {
        Vector2 center =
            attackPoint != null ? (Vector2)attackPoint.position : (Vector2)playerRoot.position;

        var hits = Physics2D.OverlapCircleAll(center, hitRadius, enemyLayer);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null)
                continue;
            enemy.TakeDamage(damage);
            RunStats.Instance?.AddDamageDealt(damage);
        }
    }

    void OnDrawGizmos()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);
        Gizmos.DrawWireSphere(attackPoint.position, hitRadius);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
        Gizmos.DrawSphere(attackPoint.position, hitRadius);
    }
}
