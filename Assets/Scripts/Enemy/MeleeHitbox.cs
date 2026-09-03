using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    [Tooltip(
        "EnemyController 를 가진 적은 이 값을 무시하고 EnemyController.damage 를 사용한다. "
        + "보스처럼 컨트롤러가 다른 경우에만 이 값이 쓰인다."
    )]
    public float damage = 10f;
    public float knockbackForce = 10f;

    private Collider2D col;
    private EnemyController owner;
    private bool hasHitThisActivation;
    private bool wasEnabled;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;
        owner = GetComponentInParent<EnemyController>();

        // 데미지 단일 출처: 근접 적의 피해량이 두 군데에 따로 적혀 어긋나는 것을 막는다.
        // (인스펙터에서 EnemyController.damage 를 고쳐도 근접 피해가 안 바뀌던 문제)
        if (owner != null)
            damage = owner.damage;
    }

    void LateUpdate()
    {
        // EnemyController.EnableHitbox 같은 외부 토글도 감지해서 활성화 시 1회 발동 잠금 리셋
        if (col.enabled && !wasEnabled)
            hasHitThisActivation = false;
        wasEnabled = col.enabled;
    }

    public void Activate(float duration)
    {
        col.enabled = true;
        hasHitThisActivation = false;
        Invoke(nameof(Deactivate), duration);
    }

    public void ForceDeactivate()
    {
        CancelInvoke(nameof(Deactivate));
        col.enabled = false;
    }

    void Deactivate() => col.enabled = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!col.enabled || hasHitThisActivation)
            return;
        if (!other.CompareTag("Player"))
            return;
        if (owner != null && owner.IsDead)
            return;
        hasHitThisActivation = true;
        other.GetComponentInParent<IDamageable>()?
            .TakeDamage(damage, owner != null ? owner.gameObject : null);

        var playerCtrl = other.GetComponentInParent<PlayerController>();
        if (playerCtrl != null)
        {
            Vector2 dir = (other.transform.root.position - transform.position).normalized;
            playerCtrl.Knockback(new Vector2(dir.x, 0.4f).normalized * knockbackForce);
        }
    }

    void OnDrawGizmos()
    {
        var c = col != null ? col : GetComponent<Collider2D>();
        if (c == null)
            return;

        bool active = c.enabled;
        Gizmos.color = active ? new Color(1f, 0.1f, 0.1f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.3f);
        var b = c.bounds;
        Gizmos.DrawWireCube(b.center, b.size);
        if (active)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
