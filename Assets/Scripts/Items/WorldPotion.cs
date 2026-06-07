using System.Collections.Generic;
using UnityEngine;

public class WorldPotion : MonoBehaviour
{
    public static readonly List<WorldPotion> Instances = new List<WorldPotion>();

    public float healAmount = 20f;
    public float pickupRadius = 1.2f;
    public float magnetRadius = 4f;
    public float magnetSpeed = 6f;

    private Transform player;
    private PlayerHealth health;

    private Vector2 velocity;
    private float gravity = 20f;
    private float groundY;
    private bool launched;
    private bool grounded;

    public void Launch(Vector2 force, float floorY)
    {
        velocity = force;
        groundY = floorY;
        launched = true;
    }

    void OnEnable() => Instances.Add(this);

    void OnDisable() => Instances.Remove(this);

    void Start()
    {
        // 포션의 모든 콜라이더(자식 포함)를 트리거로 — 물리 충돌 방지
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.isTrigger = true;

        // 자식의 Rigidbody2D를 Kinematic으로 — 스크립트가 직접 위치를 제어
        foreach (var rb in GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // 플레이어의 모든 콜라이더(자식 포함)와 충돌 무시
        if (PlayerRef.Exists)
        {
            var playerCols = PlayerRef.GameObject.GetComponentsInChildren<Collider2D>(true);
            foreach (var myCol in GetComponentsInChildren<Collider2D>(true))
            foreach (var pc in playerCols)
                if (pc != null)
                    Physics2D.IgnoreCollision(myCol, pc, true);
        }

        if (!launched)
            SnapToGround();
    }

    void Update()
    {
        if (launched)
        {
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;

            if (velocity.y < 0f && transform.position.y <= groundY)
            {
                transform.position = new Vector3(
                    transform.position.x,
                    groundY,
                    transform.position.z
                );
                launched = false;
                grounded = true;
            }

            if (!grounded && velocity.y < 0f)
            {
                float realGround = FindGroundY();
                if (transform.position.y <= realGround)
                {
                    transform.position = new Vector3(
                        transform.position.x,
                        realGround,
                        transform.position.z
                    );
                    launched = false;
                    grounded = true;
                }
            }

            return;
        }

        if (!grounded)
            SnapToGround();

        if (player == null)
        {
            if (!PlayerRef.Exists)
                return;
            player = PlayerRef.Transform;
            health = PlayerRef.Health;
        }

        if (health == null || health.IsDead)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= pickupRadius)
        {
            health.Heal(healAmount);
            Destroy(gameObject);
            return;
        }

        bool needsHeal = health.CurrentHp < health.EffectiveMaxHp;
        if (needsHeal && dist <= magnetRadius)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * magnetSpeed * Time.deltaTime;
        }
    }

    void SnapToGround()
    {
        float y = FindGroundY();
        if (y < transform.position.y)
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        grounded = true;
    }

    float FindGroundY()
    {
        var hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            20f,
            LayerMask.GetMask("Ground", "Platform")
        );
        if (hit.collider != null)
            return hit.point.y;
        return groundY;
    }
}
