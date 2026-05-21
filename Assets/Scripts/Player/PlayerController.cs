using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerController : MonoBehaviour
{
    [Header("Death")]
    [SerializeField]
    private float deathReloadDelay = 3f;

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
                StartCoroutine(ReloadAfterDelay(deathReloadDelay));
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

    IEnumerator ReloadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
