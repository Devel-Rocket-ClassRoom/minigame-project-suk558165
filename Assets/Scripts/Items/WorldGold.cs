using UnityEngine;

public class WorldGold : MonoBehaviour
{
    public int amount = 5;
    public float magnetRadius = 3f; // 이 거리 안에 들어오면 날아옴
    public float pickupRadius = 0.4f; // 이 거리에서 획득
    public float magnetSpeed = 8f;

    private Transform player;
    private Inventory inventory;

    void Update()
    {
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
