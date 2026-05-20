using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;

    private float hp;

    public bool IsDead => hp <= 0f;

    void Awake()
    {
        hp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead)
            return;
        hp -= amount;
        Debug.Log($"[Player] 피격 -{amount}  HP: {Mathf.Max(hp, 0):F0} / {maxHp:F0}");
    }
}
