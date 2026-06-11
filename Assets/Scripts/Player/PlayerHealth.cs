using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    /// <summary>체력(또는 최대체력)이 변할 때 발행: (현재 HP, 유효 최대 HP). UI가 구독.</summary>
    public static event Action<float, float> OnHealthChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => OnHealthChanged = null;

    public float maxHp = 100f;

    [Header("Audio")]
    public AudioClip deathSound;

    [Header("부활 무적 시간(초)")]
    public float reviveInvulnDuration = 1f;

    private float hp;
    private float invulnTimer;
    private Inventory inventory;
    private PlayerMovement movement;

    public bool IsDead => hp <= 0f;
    public float CurrentHp => hp;
    public float EffectiveMaxHp =>
        maxHp + (inventory != null ? inventory.GetTotalStatBonus().maxHp : 0f);

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        movement = GetComponent<PlayerMovement>();
        hp = maxHp;
        PlayerRef.Register(this);

        // 장신구 장착 등으로 EffectiveMaxHp가 바뀌면 체력바 최대치도 갱신되도록 인벤토리 변경을 체력 이벤트로 재발행한다.
        if (inventory != null)
            inventory.OnInventoryChanged += NotifyHealth;
        NotifyHealth();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= NotifyHealth;
        PlayerRef.Clear(this);
    }

    void NotifyHealth() => OnHealthChanged?.Invoke(hp, EffectiveMaxHp);

    void Update()
    {
        if (invulnTimer > 0f)
            invulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (IsDead)
            return;

        // 대쉬 중 / 부활 무적
        if ((movement != null && movement.IsDashing) || invulnTimer > 0f)
            return;

        var bonus = inventory?.GetTotalStatBonus() ?? default;

        if (bonus.evasionRate > 0f && UnityEngine.Random.value < bonus.evasionRate)
            return;

        float finalDamage =
            amount * (1f + bonus.damageReceivedMult) * (1f - Mathf.Clamp01(bonus.damageReduction));

        hp -= finalDamage;
        RunStats.Instance?.AddDamageTaken(finalDamage);

        // 반사: 받은 데미지의 일부를 공격자에게 되돌려줌 (반사 데미지는 다시 반사되지 않도록 attacker 미전달)
        if (attacker != null && bonus.reflectRatio > 0f)
            attacker.GetComponent<IDamageable>()?.TakeDamage(finalDamage * bonus.reflectRatio);

        DamagePopup.Spawn(transform.position + Vector3.up * 0.5f, finalDamage, isPlayerDamage: true);
        ScreenHitEffect.Instance?.Flash();

        // 1회성 부활: 죽을 데미지를 받았으나 부활 강화가 남아있으면 체력을 복구한다.
        if (hp <= 0f && MetaUpgrades.CanRevive)
        {
            MetaUpgrades.ConsumeRevive();
            hp = EffectiveMaxHp;
            invulnTimer = reviveInvulnDuration;
            NotifyHealth();
            return;
        }

        if (IsDead)
        {
            RunStats.Instance?.AddDeath();
            AudioManager.Instance?.PlaySFX(deathSound);
        }

        NotifyHealth();
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;
        hp = Mathf.Min(hp + amount, EffectiveMaxHp);
        NotifyHealth();
    }

    /// <summary>마을 귀환 시 HP를 최대치로 복구합니다.</summary>
    public void Revive()
    {
        hp = EffectiveMaxHp;
        NotifyHealth();
    }
}
