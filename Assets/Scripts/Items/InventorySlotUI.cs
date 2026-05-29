using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SlotType
{
    Weapon,
    Accessory,
    Backpack,
}

public class InventorySlotUI
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
{
    [SerializeField]
    private Image background;

    [SerializeField]
    private Image icon;

    [SerializeField]
    private Image highlight;

    [HideInInspector]
    public SlotType slotType;

    [HideInInspector]
    public int slotIndex;

    private bool isEmpty = true;

    public bool IsEmpty => isEmpty;

    public static InventorySlotUI DragSource { get; private set; }
    public static event Action<InventorySlotUI, InventorySlotUI> OnSlotDropped;
    public static event Action<InventorySlotUI> OnDragStarted;
    public static event Action OnDragEnded;

    public void Setup(Image bg, Image ic, Image hl)
    {
        background = bg;
        icon = ic;
        highlight = hl;
        Clear();
    }

    public void SetItem(Sprite itemIcon)
    {
        if (icon == null)
            return;
        isEmpty = false;
        icon.sprite = itemIcon;
        icon.enabled = true;
        icon.color = Color.white;
    }

    public Sprite GetIcon()
    {
        return isEmpty || icon == null ? null : icon.sprite;
    }

    public void Clear()
    {
        isEmpty = true;
        if (icon == null)
            return;
        icon.sprite = null;
        icon.enabled = false;
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty)
        {
            eventData.pointerDrag = null;
            return;
        }

        DragSource = this;
        icon.raycastTarget = false;
        OnDragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragSource == this)
        {
            icon.raycastTarget = true;
            DragSource = null;
            OnDragEnded?.Invoke();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragSource == null || DragSource == this)
            return;

        OnSlotDropped?.Invoke(DragSource, this);
    }
}
