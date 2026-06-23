using System.Collections.Generic;
using UnityEngine;

public class WorldGold : MonoBehaviour
{
    public static readonly List<WorldGold> Instances = new List<WorldGold>();

    public int amount = 5;
    public float magnetRadius = 3f;
    public float pickupRadius = 0.8f;
    public AudioClip pickupSound;
    public float magnetSpeed = 8f;

    private Transform player;
    private Inventory inventory;

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
        if (!launched)
            SnapToGround();
    }

    void Update()
    {
        FindPlayer();

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
        }

        if (!grounded && !launched)
            SnapToGround();

        if (player == null || inventory == null)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= pickupRadius)
        {
            AudioManager.Instance?.PlaySFX(pickupSound);
            float goldDrop = inventory.GetTotalStatBonus().goldDrop;
            inventory.AddGold(Mathf.RoundToInt(amount * (1f + goldDrop)));
            Destroy(gameObject);
            return;
        }

        if (!launched && dist <= magnetRadius)
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

    void FindPlayer()
    {
        if (player != null)
            return;
        if (!PlayerRef.Exists)
            return;
        player = PlayerRef.Transform;
        inventory = PlayerRef.Inventory;
    }
}
