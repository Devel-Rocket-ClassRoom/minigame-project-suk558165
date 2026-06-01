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
    private Inventory inventory;

    void Awake()
    {
        weaponSr = GetComponent<SpriteRenderer>();
        playerRoot = transform.root;
        visuals = playerRoot.Find("Visuals");
        inventory = playerRoot.GetComponent<Inventory>();

        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");
    }

    public void ApplyWeaponData(WeaponData data)
    {
        damage = data.damage;
        if (weaponSr != null)
            weaponSr.sprite = data.sprite;

        float s = data.spriteScale > 0f ? data.spriteScale : 1f;
        transform.localScale = new Vector3(s, s, 1f);
        transform.localPosition = new Vector3(
            data.spriteOffset.x,
            data.spriteOffset.y,
            transform.localPosition.z
        );
        transform.localEulerAngles = new Vector3(0f, 0f, data.spriteRotation);
    }

    public void OnHitFrame()
    {
        Vector2 center =
            attackPoint != null ? (Vector2)attackPoint.position : (Vector2)playerRoot.position;

        var bonus = inventory?.GetTotalStatBonus() ?? default;
        float effectiveDamage = (damage + bonus.damage) * (1f + bonus.damageDealtMult);
        if (bonus.criticalChance > 0f && Random.value < bonus.criticalChance)
            effectiveDamage *= 1f + bonus.criticalDamage;

        var hits = Physics2D.OverlapCircleAll(center, hitRadius, enemyLayer);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null)
                continue;
            enemy.TakeDamage(effectiveDamage);
            RunStats.Instance?.AddDamageDealt(effectiveDamage);
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
