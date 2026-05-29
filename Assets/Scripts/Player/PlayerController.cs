using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug")]
    public bool previewDeath;

    private PlayerHealth health;
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private bool deathHandled;

    private static readonly int HashIsDead = Animator.StringToHash("IsDead");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        bool dead = (health != null && health.IsDead) || previewDeath;
        if (dead)
        {
            animator.SetBool(HashIsDead, true);
            if (!deathHandled)
            {
                deathHandled = true;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            return;
        }

        movement.HandleInput();
        combat.HandleInput();
        movement.UpdateAnimatorAndFlip(combat.IsAttacking);
    }

    void FixedUpdate()
    {
        movement.FixedUpdateMovement();
    }

    /// <summary>
    /// 마을 귀환 시 죽음 상태를 완전히 초기화합니다.
    /// HP 복구 + 물리/애니메이터 상태 복원
    /// </summary>
    public void Knockback(Vector2 velocity)
    {
        if (health == null || health.IsDead)
            return;
        movement.ApplyKnockback(velocity);
    }

    public void Revive()
    {
        health?.Revive();

        deathHandled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        animator.SetBool(HashIsDead, false);
    }
}
