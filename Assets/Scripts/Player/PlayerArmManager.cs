using UnityEngine;

public class PlayerArmManager : MonoBehaviour
{
    [Header("Arm Renderers")]
    public SpriteRenderer rightArmRenderer;
    public SpriteRenderer leftArmRenderer;

    [Header("Sprites")]
    public Sprite rightArmSprite;
    public Sprite leftArmSprite;

    private Animator animator;
    private bool isAttacking;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (rightArmRenderer != null && rightArmSprite != null)
            rightArmRenderer.sprite = rightArmSprite;

        if (leftArmRenderer != null)
            leftArmRenderer.enabled = false;
    }

    void Update()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool attacking = stateInfo.IsTag("Attack");

        if (attacking != isAttacking)
        {
            isAttacking = attacking;

            if (leftArmRenderer != null)
            {
                leftArmRenderer.enabled = isAttacking;
                if (isAttacking && leftArmSprite != null)
                    leftArmRenderer.sprite = leftArmSprite;
            }
        }
    }
}
