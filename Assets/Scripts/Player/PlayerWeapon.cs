using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Hit Detection")]
    public float attackRadius = 1.5f;
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

    // 애니메이션 이벤트에서 호출
    public void OnHitFrame()
    {
        bool facingLeft = visuals != null && visuals.localScale.x < 0f;
        float facing = facingLeft ? -1f : 1f;
        Vector2 tipPos = (Vector2)playerRoot.position + new Vector2(facing * attackRadius, 0.4f);

        var hits = Physics2D.OverlapCircleAll(tipPos, hitRadius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
                continue;
            var damageable = hit.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
        }
    }

    void OnDrawGizmos()
    {
        Transform root = transform.root;
        Transform vis = root.Find("Visuals");
        bool facingLeft = vis != null && vis.localScale.x < 0f;
        float facing = facingLeft ? -1f : 1f;

        Vector2 origin = (Vector2)root.position;
        Vector2 tipPos = origin + new Vector2(facing * attackRadius, 0.4f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin, attackRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, tipPos);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(tipPos, hitRadius);
    }
}
