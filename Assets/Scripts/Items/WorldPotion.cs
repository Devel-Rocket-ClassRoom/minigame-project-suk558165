using UnityEngine;

public class WorldPotion : MonoBehaviour
{
    public float healAmount = 20f;
    public float pickupRadius = 1.2f;

    private Transform player;
    private PlayerHealth health;

    // ── 방출 물리 ──
    private Vector2 velocity;
    private float gravity = 20f;
    private float groundY;
    private bool launched;

    public void Launch(Vector2 force, float floorY)
    {
        velocity = force;
        groundY = floorY;
        launched = true;
    }

    void Update()
    {
        if (launched)
        {
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;

            if (velocity.y < 0f && transform.position.y <= groundY)
            {
                transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
                launched = false;
            }

            return;
        }

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null)
                return;
            player = go.transform;
            health =
                go.GetComponent<PlayerHealth>()
                ?? go.GetComponentInChildren<PlayerHealth>()
                ?? go.GetComponentInParent<PlayerHealth>();
        }

        if (health == null || health.IsDead)
            return;

        if (Vector2.Distance(transform.position, player.position) <= pickupRadius)
        {
            health.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
