using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Image highlight;
    [SerializeField] private TextMeshProUGUI label;

    private bool isEmpty = true;

    public void Setup(Image bg, Image ic, Image hl, TextMeshProUGUI lbl)
    {
        background = bg;
        icon = ic;
        highlight = hl;
        label = lbl;
        Clear();
    }

    public void SetItem(Sprite itemIcon, string itemName)
    {
        isEmpty = false;
        icon.sprite = itemIcon;
        icon.enabled = true;
        icon.color = Color.white;
        if (label != null)
            label.text = itemName;
    }

    public void Clear()
    {
        isEmpty = true;
        icon.sprite = null;
        icon.enabled = false;
        if (label != null)
            label.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null)
            highlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null)
            highlight.enabled = false;
    }
}
