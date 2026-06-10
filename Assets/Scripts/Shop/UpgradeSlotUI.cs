using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>마을 영구 강화창의 한 줄. ShopSlotUI 패턴을 따른다.</summary>
public class UpgradeSlotUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private TextMeshProUGUI costText;

    [SerializeField]
    private Button buyButton;

    private MetaUpgradeType type;
    private UpgradeShopUI shop;

    void Awake()
    {
        buyButton?.onClick.AddListener(OnBuyClick);
    }

    public void Setup(MetaUpgradeEntry entry, UpgradeShopUI ui)
    {
        type = entry.type;
        shop = ui;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(entry.nameKey)
                ? entry.fallbackName
                : L10n.Get(entry.nameKey, entry.fallbackName);

        Refresh(Inventory.Instance != null ? Inventory.Instance.Gold : 0);
        gameObject.SetActive(true);
    }

    public void Refresh(int gold)
    {
        int level = MetaUpgrades.GetLevel(type);
        int maxLevel = MetaUpgrades.GetMaxLevel(type);
        int cost = MetaUpgrades.GetNextCost(type);

        if (levelText != null)
            levelText.text = L10n.Format("ui.upgrade.level", "Lv {0}/{1}", level, maxLevel);

        bool maxed = cost < 0;
        if (costText != null)
            costText.text = maxed
                ? L10n.Get("ui.upgrade.maxed", "MAX")
                : $"{cost} G";

        if (buyButton != null)
            buyButton.interactable = !maxed && gold >= cost;
    }

    void OnBuyClick()
    {
        if (shop == null)
            return;
        shop.TryUpgrade(type);
    }
}
