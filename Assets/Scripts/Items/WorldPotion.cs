using UnityEngine;

public class WorldPotion : MonoBehaviour
{
    public float healAmount = 20f;
    public float pickupRadius = 1.2f;

    private Transform player;
    private PlayerHealth health;

    void Update()
    {
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
