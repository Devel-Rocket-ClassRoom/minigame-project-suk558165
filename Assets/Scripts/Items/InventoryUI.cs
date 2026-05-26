using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Sprites (TravelBookLite)")]
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Sprite slotHighlightSprite;
    [SerializeField] private Sprite coinIconSprite;

    private Inventory inventory;
    private GameObject panel;
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI detailNameText;
    private TextMeshProUGUI detailDescText;
    private Image detailIcon;
    private List<SlotEntry> weaponSlots = new List<SlotEntry>();
    private List<SlotEntry> accessorySlots = new List<SlotEntry>();
    private bool isOpen;

    struct SlotEntry
    {
        public Image icon;
        public Image highlight;
    }

    void Start()
    {
        BuildUI();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    void Open()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null) return;
        }

        isOpen = true;
        panel.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
    }

    void Close()
    {
        isOpen = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Refresh()
    {
        if (inventory == null) return;

        var weapons = inventory.WeaponInventory;
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weapons != null && i < weapons.weapons.Count)
            {
                weaponSlots[i].icon.sprite = weapons.weapons[i].sprite;
                weaponSlots[i].icon.enabled = weapons.weapons[i].sprite != null;
            }
            else
            {
                weaponSlots[i].icon.enabled = false;
            }
        }

        for (int i = 0; i < accessorySlots.Count; i++)
        {
            if (i < inventory.Accessories.Count)
            {
                accessorySlots[i].icon.sprite = inventory.Accessories[i].icon;
                accessorySlots[i].icon.enabled = inventory.Accessories[i].icon != null;
            }
            else
            {
                accessorySlots[i].icon.enabled = false;
            }
        }

        goldText.text = inventory.Gold.ToString();
    }

    void BuildUI()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        panel = CreatePanel(transform);

        var frame = CreateFrame(panel.transform);

        var title = CreateTMP(frame.transform, "INVENTORY", 28, FontStyles.Bold,
            new Color(1f, 0.9f, 0.7f));
        SetRect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -30), new Vector2(300, 40));

        var weaponLabel = CreateTMP(frame.transform, "WEAPONS", 18, FontStyles.Bold,
            new Color(0.9f, 0.85f, 0.7f));
        SetRect(weaponLabel, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50, -70), new Vector2(200, 30));

        var weaponGrid = CreateObj("WeaponGrid", frame.transform);
        SetRect(weaponGrid, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50, -200), new Vector2(240, 120));
        AddGridLayout(weaponGrid, new Vector2(100, 100), new Vector2(10, 10));

        for (int i = 0; i < 2; i++)
            weaponSlots.Add(CreateSlot(weaponGrid.transform));

        var accLabel = CreateTMP(frame.transform, "ACCESSORIES", 18, FontStyles.Bold,
            new Color(0.9f, 0.85f, 0.7f));
        SetRect(accLabel, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-250, -70), new Vector2(200, 30));

        var accGrid = CreateObj("AccessoryGrid", frame.transform);
        SetRect(accGrid, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-250, -200), new Vector2(240, 240));
        AddGridLayout(accGrid, new Vector2(100, 100), new Vector2(10, 10));

        for (int i = 0; i < Inventory.MaxAccessories; i++)
            accessorySlots.Add(CreateSlot(accGrid.transform));

        var detailPanel = CreateObj("DetailPanel", frame.transform);
        SetRect(detailPanel, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(30, 20), new Vector2(-60, 80));

        detailIcon = CreateImage(detailPanel.transform, null);
        SetRect(detailIcon.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10, 0), new Vector2(60, 60));
        detailIcon.enabled = false;

        detailNameText = CreateTMP(detailPanel.transform, "", 20, FontStyles.Bold,
            Color.white);
        SetRect(detailNameText.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(80, 10), new Vector2(-90, 30));

        detailDescText = CreateTMP(detailPanel.transform, "", 16, FontStyles.Normal,
            new Color(0.8f, 0.8f, 0.8f));
        SetRect(detailDescText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(80, 5), new Vector2(-90, 25));

        var goldRow = CreateObj("GoldRow", frame.transform);
        SetRect(goldRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 105), new Vector2(160, 30));

        if (coinIconSprite != null)
        {
            var coinImg = CreateImage(goldRow.transform, coinIconSprite);
            SetRect(coinImg.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0, 0), new Vector2(28, 28));
        }

        goldText = CreateTMP(goldRow.transform, "0", 22, FontStyles.Bold,
            new Color(1f, 0.85f, 0.4f));
        SetRect(goldText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(34, 0), new Vector2(-34, 0));
        goldText.alignment = TextAlignmentOptions.Left;

        var hint = CreateTMP(panel.transform, "TAB to close", 16, FontStyles.Italic,
            new Color(0.6f, 0.6f, 0.6f));
        SetRect(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 30), new Vector2(200, 30));
    }

    GameObject CreatePanel(Transform parent)
    {
        var go = CreateObj("InventoryPanel", parent);
        SetRect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.75f);
        img.raycastTarget = true;
        return go;
    }

    GameObject CreateFrame(Transform parent)
    {
        var go = CreateObj("Frame", parent);
        SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(700, 500));
        var img = go.AddComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
        }
        else
        {
            img.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);
        }
        return go;
    }

    SlotEntry CreateSlot(Transform parent)
    {
        var go = CreateObj("Slot", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        var bg = go.AddComponent<Image>();
        if (slotSprite != null)
        {
            bg.sprite = slotSprite;
            bg.type = Image.Type.Sliced;
        }
        else
        {
            bg.color = new Color(0.2f, 0.18f, 0.15f, 0.9f);
        }

        var hlGo = CreateObj("Highlight", go.transform);
        SetRect(hlGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var hl = hlGo.AddComponent<Image>();
        if (slotHighlightSprite != null)
        {
            hl.sprite = slotHighlightSprite;
            hl.type = Image.Type.Sliced;
        }
        else
        {
            hl.color = new Color(1f, 0.9f, 0.5f, 0.3f);
        }
        hl.enabled = false;

        var iconGo = CreateObj("Icon", go.transform);
        SetRect(iconGo, Vector2.zero, Vector2.one,
            new Vector2(8, 8), new Vector2(-16, -16));
        var icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;
        icon.raycastTarget = false;

        return new SlotEntry { icon = icon, highlight = hl };
    }

    GameObject CreateObj(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = 5;
        return go;
    }

    void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    Image CreateImage(Transform parent, Sprite sprite)
    {
        var go = CreateObj("Image", parent);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string text, float size,
        FontStyles style, Color color)
    {
        var go = CreateObj("Text", parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    void AddGridLayout(GameObject go, Vector2 cellSize, Vector2 spacing)
    {
        var grid = go.AddComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.childAlignment = TextAnchor.UpperLeft;
    }
}
