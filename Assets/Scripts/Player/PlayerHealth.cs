using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;

    private float hp;
    private Inventory inventory;

    public bool IsDead => hp <= 0f;
    public float CurrentHp => hp;
    public float EffectiveMaxHp => maxHp + (inventory != null ? inventory.GetTotalStatBonus().maxHp : 0f);

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
    }
}
