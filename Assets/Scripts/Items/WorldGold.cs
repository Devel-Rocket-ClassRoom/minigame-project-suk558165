using UnityEngine;

public class WorldGold : MonoBehaviour
{
    public int amount = 5;
    public float magnetRadius = 3f;
    public float pickupRadius = 0.8f;
    public float magnetSpeed = 8f;

    private Transform player;
    private Inventory inventory;

    // ── 방출 물리 ──
    private Vector2 velocity;
    private float gravity = 20f;
    private float groundY;
    private bool launched;

    /// <summary>
    /// 부채꼴로 방출할 때 호출.
    /// 스폰 위치의 Y를 바닥으로 기억하고, 낙하 시 바닥에서 멈춤.
    /// </summary>
    public void Launch(Vector2 force, float floorY)
    {
        velocity = force;
        groundY = floorY;
        launched = true;
    }

    void Update()
    {
        // 방출 물리 처리
        if (launched)
        {
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;

            // 바닥에 닿으면 착지
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
            inventory =
                go.GetComponent<Inventory>()
                ?? go.GetComponentInChildren<Inventory>()
                ?? go.GetComponentInParent<Inventory>();
        }

        if (inventory == null)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= pickupRadius)
        {
            inventory.AddGold(amount);
            Destroy(gameObject);
            return;
        }

        if (dist <= magnetRadius)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * magnetSpeed * Time.deltaTime;
        }
    }
}
