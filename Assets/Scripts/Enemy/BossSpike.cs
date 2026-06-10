using UnityEngine;

/// <summary>
/// 보스 가시 프리팹에 붙이는 컴포넌트.
/// 프리팹에 SpriteRenderer + Collider2D(IsTrigger) 와 함께 두고,
/// 보스가 Instantiate 후 Init() 으로 데미지/주인을 주입한다.
/// 플레이어에 1회 데미지 + 위로 넉백 후 activeTime 뒤 자동 소멸.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossSpike : MonoBehaviour
{
    [Tooltip("솟은 뒤 사라지기까지 시간(초)")]
    [SerializeField]
    private float activeTime = 0.5f;

    [Tooltip("플레이어가 맞았을 때 위로 넉백되는 세기")]
    [SerializeField]
    private float knockbackForce = 9f;

    private float damage;
    private GameObject owner;
    private bool hit;

    public void Init(float damage, GameObject owner)
    {
        this.damage = damage;
        this.owner = owner;
        Destroy(gameObject, activeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit)
            return;
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null)
            return;
        hit = true;
        other.GetComponentInParent<IDamageable>()?.TakeDamage(damage, owner);
        pc.Knockback(Vector2.up * knockbackForce);
    }
}
