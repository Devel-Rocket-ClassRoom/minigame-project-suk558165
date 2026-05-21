using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWeapon : MonoBehaviour
{
    [Header("Swing")]
    public float swingAngle = 120f;
    public float swingDuration = 0.5f;
    public float restAngle = -90f;
    public float attackRadius = 1.5f;
    public float hitRadius = 0.7f;

    [Header("Damage")]
    public float damage = 20f;
    public LayerMask enemyLayer;

    private bool isSwinging;
    private SpriteRenderer weaponSr;
    private SpriteRenderer parentSr;
    private Transform playerRoot;
    private Transform visuals;
    private readonly HashSet<int> hitIds = new HashSet<int>();

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        weaponSr = GetComponentInChildren<SpriteRenderer>();
        parentSr =
            transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;

        playerRoot = transform.root;
        visuals = playerRoot.Find("Visuals");

        var col = GetComponentInChildren<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0f, 0f, restAngle);
    }

    public bool Swinging => isSwinging;

    public void ApplyWeaponData(WeaponData data)
    {
        damage = data.damage;
        swingAngle = data.swingAngle;
        swingDuration = data.swingDuration;
        if (weaponSr != null)
            weaponSr.sprite = data.sprite;
    }

    public void Attack(Vector3 mouseWorld)
    {
        if (isSwinging || !gameObject.activeInHierarchy)
            return;
        StartCoroutine(DoSwing(mouseWorld));
    }

    IEnumerator DoSwing(Vector3 mouseWorld)
    {
        isSwinging = true;
        hitIds.Clear();

        Vector2 dir = (Vector2)(mouseWorld - playerRoot.position);
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bool facingLeft = Mathf.Abs(baseAngle) > 90f;
        if (weaponSr != null)
            weaponSr.flipY = facingLeft;

        float half = swingAngle / 2f;
        float from = baseAngle + half;
        float to = baseAngle - half;

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / swingDuration);
            float currentAngle = Mathf.Lerp(from, to, ratio);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            CheckHit(currentAngle);

            yield return null;
        }

        isSwinging = false;
        transform.localRotation = Quaternion.Euler(0f, 0f, restAngle);
    }

    void CheckHit(float angleDeg)
    {
        bool facingLeft = visuals != null && visuals.localScale.x < 0f;
        float facing = facingLeft ? -1f : 1f;
        Vector2 origin = (Vector2)playerRoot.position;
        Vector2 tipPos = origin + new Vector2(facing * attackRadius, 0.2f);

        var hits = Physics2D.OverlapCircleAll(tipPos, hitRadius, enemyLayer);
        foreach (var hit in hits)
        {
            int id = hit.GetInstanceID();
            if (hitIds.Contains(id))
                continue;
            if (hit.CompareTag("Player"))
                continue;

            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            hitIds.Add(id);
            damageable.TakeDamage(damage);
            Debug.Log("[PlayerWeapon] " + hit.name + " hit — damage " + damage);
        }
    }

    void Update()
    {
        if (isSwinging || parentSr == null)
            return;
        if (weaponSr != null)
            weaponSr.flipY = parentSr.flipX;
    }

    void OnDrawGizmos()
    {
        Transform root = transform.root;
        Transform vis = root.Find("Visuals");
        bool facingLeft = vis != null && vis.localScale.x < 0f;
        float facing = facingLeft ? -1f : 1f;

        Vector2 origin = (Vector2)root.position;
        Vector2 tipPos = origin + new Vector2(facing * attackRadius, 0.2f);

        // 무기 사거리
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin, attackRadius);

        // 플레이어 → 무기 끝 선
        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, tipPos);

        // 실제 피격 판정 원
        Gizmos.color = isSwinging ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(tipPos, hitRadius);
    }
}
