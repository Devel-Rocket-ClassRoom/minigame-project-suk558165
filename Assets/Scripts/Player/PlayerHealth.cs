using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;

    private float hp;
    private Inventory inventory;

    public bool IsDead => hp <= 0f;
    public float CurrentHp => hp;
    public float EffectiveMaxHp =>
        maxHp + (inventory != null ? inventory.GetTotalStatBonus().maxHp : 0f);

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        hp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead)
            return;
        hp -= amount;
        RunStats.Instance?.AddDamageTaken(amount);
        if (IsDead)
            RunStats.Instance?.AddDeath();
    }

    /// <summary>마을 귀환 시 HP를 최대치로 복구합니다.</summary>
    public void Revive()
    {
        hp = EffectiveMaxHp;
    }
}
