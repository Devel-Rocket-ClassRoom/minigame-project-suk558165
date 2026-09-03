using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Hit Detection")]
    [Tooltip("몸통 앞으로 뻗는 사거리(월드 유닛). 무기 데이터에 hitRange가 있으면 그 값이 우선")]
    public float hitRange = 1.4f;

    [Tooltip("판정이 덮는 세로 높이. 플레이어 몸 높이에 맞춰야 낮은 적도 맞는다")]
    public float hitHeight = 1.9f;

    [Tooltip("판정 박스 중심의 높이(플레이어 발 기준)")]
    public float hitYOffset = 0.95f;

    [Tooltip("몸통 반폭. 이 지점부터 사거리를 뻗는다")]
    public float bodyHalfWidth = 0.42f;

    [Tooltip("히트 프레임 이후 판정이 열려 있는 시간. 0이면 1프레임만 검사")]
    public float hitWindow = 0.12f;

    public float damage = 20f;
    public LayerMask enemyLayer;

    private SpriteRenderer weaponSr;
    private Transform playerRoot;
    private PlayerMovement movement;
    private Inventory inventory;
    private PlayerHealth health;

    private float windowTimeLeft;
    private readonly HashSet<int> hitThisSwing = new HashSet<int>();
    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    // 현재 스윙의 데미지 — 판정 창 동안 매 프레임 다시 굴리지 않도록 한 번만 계산한다.
    private float pendingDamage;
    private float pendingLifesteal;

    void Awake()
    {
        weaponSr = GetComponent<SpriteRenderer>();
        playerRoot = transform.root;
        movement = playerRoot.GetComponent<PlayerMovement>();
        inventory = playerRoot.GetComponent<Inventory>();
        health = playerRoot.GetComponent<PlayerHealth>();

        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");

        // 기본 상태에서 무기 숨김 (공격 시에만 표시)
        if (weaponSr != null)
            weaponSr.enabled = false;
    }

    public void Show()
    {
        if (weaponSr != null)
            weaponSr.enabled = true;
    }

    public void Hide()
    {
        if (weaponSr != null)
            weaponSr.enabled = false;
    }

    public void ApplyWeaponData(WeaponData data)
    {
        damage = data.damage;
        currentWeaponRange = data.hitRange;

        if (weaponSr != null)
            weaponSr.sprite = data.sprite;

        float s = data.spriteScale > 0f ? data.spriteScale : 1f;
        transform.localScale = new Vector3(s, s, 1f);

        if (data.weaponType == WeaponType.Magic)
        {
            // Magic은 애니메이션 클립에서 position/rotation을 키프레임으로 직접 제어
            // 코드에서 덮어쓰지 않도록 (0,0,0)으로 초기화만 해둠
            transform.localPosition = new Vector3(0f, 0f, transform.localPosition.z);
            transform.localEulerAngles = Vector3.zero;
        }
        else
        {
            transform.localPosition = new Vector3(
                data.spriteOffset.x,
                data.spriteOffset.y,
                transform.localPosition.z
            );
            transform.localEulerAngles = new Vector3(0f, 0f, data.spriteRotation);
        }
    }

    private float currentWeaponRange;

    float EffectiveRange => currentWeaponRange > 0f ? currentWeaponRange : hitRange;

    /// <summary>Animation Event — 히트 프레임에서 호출. 판정 창을 연다.</summary>
    public void OnHitFrame()
    {
        hitThisSwing.Clear();

        var bonus = inventory?.GetTotalStatBonus() ?? default;
        float dmg = (damage + bonus.damage) * (1f + bonus.damageDealtMult);
        dmg *= CombatMath.LowHpMultiplier(health, bonus.lowHpDamageBonus);
        if (bonus.criticalChance > 0f && Random.value < bonus.criticalChance)
            dmg *= 1f + bonus.criticalDamage;

        pendingDamage = dmg;
        pendingLifesteal = bonus.lifesteal;

        // 창을 열고 즉시 1회 검사 — hitWindow가 0이어도 기존처럼 동작한다.
        windowTimeLeft = hitWindow;
        SampleHits();
    }

    void FixedUpdate()
    {
        if (windowTimeLeft <= 0f)
            return;

        windowTimeLeft -= Time.fixedDeltaTime;
        SampleHits();
    }

    /// <summary>
    /// 판정 창이 열려 있는 동안 매 물리 프레임 검사한다.
    /// 단일 프레임 검사만 하면 적이 그 순간 반경 밖에 있을 때 통째로 헛스윙이 된다.
    /// </summary>
    void SampleHits()
    {
        GetHitBox(out Vector2 center, out Vector2 size);

        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, overlapBuffer, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            var hit = overlapBuffer[i];
            if (hit == null)
                continue;

            // 허트박스가 자식에 있을 수 있으므로 부모까지 올라가서 찾는다.
            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            var targetObj = (damageable as Component)?.gameObject;
            int id = targetObj != null ? targetObj.GetInstanceID() : hit.GetInstanceID();

            // 콜라이더가 여러 개인 적을 한 스윙에 중복 타격하지 않도록 대상 기준으로 막는다.
            if (!hitThisSwing.Add(id))
                continue;

            damageable.TakeDamage(pendingDamage, playerRoot.gameObject);
            RunStats.Instance?.AddDamageDealt(pendingDamage);

            if (pendingLifesteal > 0f)
                health?.Heal(pendingDamage * pendingLifesteal);
        }
    }

    /// <summary>바라보는 방향으로 몸통 앞을 덮는 판정 박스.</summary>
    void GetHitBox(out Vector2 center, out Vector2 size)
    {
        float facing = FacingSign();
        float range = EffectiveRange;

        center = new Vector2(
            playerRoot.position.x + facing * (bodyHalfWidth + range * 0.5f),
            playerRoot.position.y + hitYOffset
        );
        size = new Vector2(range, hitHeight);
    }

    float FacingSign()
    {
        if (movement != null && movement.Visuals != null)
            return movement.Visuals.localScale.x < 0f ? -1f : 1f;
        if (movement != null && movement.Sr != null)
            return movement.Sr.flipX ? -1f : 1f;
        return 1f;
    }

    void OnDrawGizmos()
    {
        if (playerRoot == null)
            playerRoot = transform.root;
        if (playerRoot == null)
            return;

        GetHitBox(out Vector2 center, out Vector2 size);

        bool active = windowTimeLeft > 0f;
        Gizmos.color = active
            ? new Color(1f, 0.2f, 0f, 0.35f)
            : new Color(1f, 0.5f, 0f, 0.12f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = active ? new Color(1f, 0.2f, 0f, 1f) : new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireCube(center, size);
    }
}
