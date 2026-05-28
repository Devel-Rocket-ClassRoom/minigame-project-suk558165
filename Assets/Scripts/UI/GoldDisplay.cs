using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI goldText;

    private Inventory inventory;

    void Update()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        if (inventory == null || goldText == null)
            return;

        goldText.text = inventory.Gold.ToString();
    }
}
