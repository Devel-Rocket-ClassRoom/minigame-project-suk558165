using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;

    [Header("Audio")]
    public AudioClip deathSound;

    private float hp;
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
    }

    void OnDestroy()
    {
        PlayerRef.Clear(this);
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (IsDead)
            return;

        // 대쉬 중 무적
        if (movement != null && movement.IsDashing)
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
        if (IsDead)
        {
            RunStats.Instance?.AddDeath();
            AudioManager.Instance?.PlaySFX(deathSound);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;
        hp = Mathf.Min(hp + amount, EffectiveMaxHp);
    }

    /// <summary>마을 귀환 시 HP를 최대치로 복구합니다.</summary>
    public void Revive()
    {
        hp = EffectiveMaxHp;
    }
}
