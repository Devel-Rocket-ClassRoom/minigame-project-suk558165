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

    [Header("Damage")]
    public float damage = 20f;
    public LayerMask enemyLayer;

    private bool isSwinging;
    private SpriteRenderer weaponSr;
    private SpriteRenderer parentSr;
    private readonly HashSet<int> hitIds = new HashSet<int>();

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        weaponSr = GetComponentInChildren<SpriteRenderer>();
        parentSr =
            transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;

        // 기존 트리거 콜라이더는 비활성화 (OverlapCircle로 대체)
        var col = GetComponentInChildren<Collider2D>();
        if (col != null)
            col.enabled = false;

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
            float currentAngle = Mathf.Lerp(from, to, ratio);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            // 매 프레임 무기 끝 위치에서 적 판정
            CheckHitAtAngle(currentAngle);

            yield return null;
        }

        isSwinging = false;
        transform.localRotation = Quaternion.Euler(0f, 0f, restAngle);
    }

    void CheckHitAtAngle(float angleDeg)
    {
        // transform.right = 무기의 월드 방향 (부모 flip 포함) → 좌우 모두 정확
        Vector2 tipPos = (Vector2)transform.position + (Vector2)transform.right * attackRadius;

        var hits = Physics2D.OverlapCircleAll(tipPos, 0.4f, enemyLayer);
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
            Debug.Log($"[PlayerWeapon] {hit.name} 피격 — 데미지 {damage}");
        }
    }

    void Update()
    {
        if (isSwinging || parentSr == null)
            return;
        if (weaponSr != null)
            weaponSr.flipY = parentSr.flipX;
    }
}
