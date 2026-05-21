using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;

    private float hp;

    public bool IsDead => hp <= 0f;
    public float CurrentHp => hp;

    void Awake()
    {
        hp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead)
            return;
        hp -= amount;
    }
}
