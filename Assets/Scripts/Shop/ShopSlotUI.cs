using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private TextMeshProUGUI priceText;

    [SerializeField]
    private Button buyButton;

    [SerializeField]
    private GameObject soldOutOverlay;

    private ScriptableObject item;
    private int price;
    private ShopUI shopUI;
    private LocalizedString trackedName;

    void Awake()
    {
        buyButton?.onClick.AddListener(OnBuyClick);
    }

    void OnDisable()
    {
        if (trackedName != null)
            trackedName.StringChanged -= OnNameChanged;
        trackedName = null;
    }

    public void Setup(ScriptableObject itemData, ShopUI ui)
    {
        shopUI = ui;
        item = itemData;

        soldOutOverlay?.SetActive(false);
        if (buyButton != null)
            buyButton.interactable = true;

        LocalizedString locName = null;
        Sprite icon = null;

        if (itemData is WeaponData weapon)
        {
            price = weapon.price;
            icon = weapon.sprite;
            locName = weapon.weaponName;
        }
        else if (itemData is AccessoryData accessory)
        {
            price = accessory.price;
            icon = accessory.icon;
            locName = accessory.accessoryName;
        }

        if (iconImage != null)
            iconImage.sprite = icon;
        if (priceText != null)
            priceText.text = $"{price} G";

        if (trackedName != null)
            trackedName.StringChanged -= OnNameChanged;
        trackedName = locName;

        bool locReady =
            locName != null
            && LocalizationSettings.HasSettings
            && LocalizationSettings.SelectedLocale != null;

        if (locReady)
        {
            try
            {
                trackedName.StringChanged += OnNameChanged;
                trackedName.RefreshString();
            }
            catch (Exception)
            {
                // Locale 미설정 시 ID 기반 이름으로 폴백
                if (nameText != null)
                    nameText.text = FallbackName(itemData);
            }
        }
        else
        {
            if (nameText != null)
                nameText.text = FallbackName(itemData);
        }

        gameObject.SetActive(true);
    }

    static string FallbackName(ScriptableObject data)
    {
        string id =
            data is WeaponData w ? w.id
            : data is AccessoryData a ? a.id
            : data.name;
        // snake_case → Title Case  (예: triple_arrow → Triple Arrow)
        var parts = id.Split('_');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0)
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
        return string.Join(" ", parts);
    }

    public void RefreshAffordable(int gold)
    {
        if (buyButton == null || !buyButton.gameObject.activeSelf)
            return;
        if (item == null)
            return;
        buyButton.interactable = gold >= price;
    }

    public void Clear()
    {
        if (trackedName != null)
        {
            trackedName.StringChanged -= OnNameChanged;
            trackedName = null;
        }
        item = null;
        if (iconImage != null)
            iconImage.sprite = null;
        if (nameText != null)
            nameText.text = "";
        if (priceText != null)
            priceText.text = "";
        gameObject.SetActive(false);
    }

    void OnNameChanged(string value)
    {
        if (nameText == null)
            return;
        if (string.IsNullOrEmpty(value) || value.StartsWith("No translation found"))
            nameText.text = item != null ? FallbackName(item) : "";
        else
            nameText.text = value;
    }

    public void MarkSoldOut()
    {
        soldOutOverlay?.SetActive(true);
        if (buyButton != null)
            buyButton.interactable = false;
        item = null;
    }

    void OnBuyClick()
    {
        if (item == null || shopUI == null)
            return;
        if (!shopUI.TryBuy(item, price))
            return;

        MarkSoldOut();
    }
}
