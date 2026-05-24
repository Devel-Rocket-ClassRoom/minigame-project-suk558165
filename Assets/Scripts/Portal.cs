using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private GameObject visualEffect;

    void Awake()
    {
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        if (visualEffect != null)
            visualEffect.SetActive(active);

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var room = FindAnyObjectByType<RoomManager>();
        if (room != null)
            room.GoToNextRoom();
    }
}
