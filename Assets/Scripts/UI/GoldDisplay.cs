using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI goldText;

    void OnEnable()
    {
        Inventory.OnGoldChanged += UpdateGold;
        // 이미 인벤토리가 존재하면 현재 골드로 즉시 1회 갱신.
        if (Inventory.Instance != null)
            UpdateGold(Inventory.Instance.Gold);
    }

    void OnDisable()
    {
        Inventory.OnGoldChanged -= UpdateGold;
    }

    void UpdateGold(int gold)
    {
        if (goldText != null)
            goldText.text = gold.ToString();
    }
}
