using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWeapon : MonoBehaviour
{
    [Header("Swing")]
    public float swingAngle = 120f;
    public float swingDuration = 0.25f;

    [Header("Damage")]
    public float damage = 20f;
    public LayerMask enemyLayer;

    private bool isSwinging;
    private Collider2D weaponCollider;
    private SpriteRenderer weaponSr;
    private readonly HashSet<int> hitIds = new HashSet<int>();

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        weaponCollider = GetComponentInChildren<Collider2D>();
        weaponSr = GetComponentInChildren<SpriteRenderer>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    public bool Swinging => isSwinging;

    public void Attack(Vector3 mouseWorld)
    {
        if (isSwinging)
            return;
        StartCoroutine(DoSwing(mouseWorld));
    }

    IEnumerator DoSwing(Vector3 mouseWorld)
    {
        isSwinging = true;
        hitIds.Clear();
        if (weaponCollider != null)
            weaponCollider.enabled = true;

        Vector2 dir = (Vector2)(mouseWorld - transform.position);
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
            float angle = Mathf.Lerp(from, to, ratio);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        if (weaponCollider != null)
            weaponCollider.enabled = false;
        isSwinging = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isSwinging)
            return;
        int id = other.GetInstanceID();
        if (hitIds.Contains(id))
            return;
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        hitIds.Add(id);
        var damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage);
    }
}
