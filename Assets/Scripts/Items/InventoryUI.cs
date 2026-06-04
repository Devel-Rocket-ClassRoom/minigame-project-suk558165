using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField]
    private List<InventorySlotUI> weaponSlots = new List<InventorySlotUI>();

    [Header("Accessory Slots")]
    [SerializeField]
    private List<InventorySlotUI> accessorySlots = new List<InventorySlotUI>();

    [Header("Backpack Slots")]
    [SerializeField]
    private List<InventorySlotUI> backpackSlots = new List<InventorySlotUI>();

    [Header("Gold")]
    [SerializeField]
    private TextMeshProUGUI goldText;

    [Header("Drag Ghost")]
    [SerializeField]
    private Image dragGhost;

    private Inventory inventory;
    private GameObject frame;
    private CanvasGroup canvasGroup;
    public static bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => IsOpen = false;

    void Start()
    {
        frame = transform.Find("Frame")?.gameObject;
        canvasGroup = GetComponent<CanvasGroup>();

        inventory = Inventory.Instance;
        InitSlotMeta();
        ClearAllSlots();
        BindEvents();

        if (dragGhost != null)
            dragGhost.gameObject.SetActive(false);

        if (frame != null)
            frame.SetActive(false);
    }

    void OnDestroy()
    {
        InventorySlotUI.OnSlotDropped -= HandleSlotDrop;
        InventorySlotUI.OnDragStarted -= HandleDragStarted;
        InventorySlotUI.OnDragEnded -= HandleDragEnded;
    }

    void InitSlotMeta()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weaponSlots[i] == null)
                continue;
            weaponSlots[i].slotType = SlotType.Weapon;
            weaponSlots[i].slotIndex = i;
        }
        for (int i = 0; i < accessorySlots.Count; i++)
        {
            if (accessorySlots[i] == null)
                continue;
            accessorySlots[i].slotType = SlotType.Accessory;
            accessorySlots[i].slotIndex = i;
        }
        for (int i = 0; i < backpackSlots.Count; i++)
        {
            if (backpackSlots[i] == null)
                continue;
            backpackSlots[i].slotType = SlotType.Backpack;
            backpackSlots[i].slotIndex = i;
        }
    }

    void ClearAllSlots()
    {
        foreach (var s in weaponSlots)
            if (s != null)
                s.Clear();
        foreach (var s in accessorySlots)
            if (s != null)
                s.Clear();
        foreach (var s in backpackSlots)
            if (s != null)
                s.Clear();
    }

    void BindEvents()
    {
        InventorySlotUI.OnSlotDropped += HandleSlotDrop;
        InventorySlotUI.OnDragStarted += HandleDragStarted;
        InventorySlotUI.OnDragEnded += HandleDragEnded;
    }

    void Update()
    {
        var inventoryKey = InputManager.Instance?.Inventory ?? KeyCode.Tab;
        if (Input.GetKeyDown(inventoryKey) && !PauseMenu.IsPaused && !ShopUI.IsOpen)
            Toggle();

        if (IsOpen && dragGhost != null && dragGhost.gameObject.activeSelf)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                frame.GetComponent<RectTransform>(),
                Input.mousePosition,
                null,
                out var localPos
            );
            dragGhost.rectTransform.anchoredPosition = localPos;
        }
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    void Open()
    {
        if (frame == null)
            return;

        if (inventory == null)
        {
            inventory = Inventory.Instance;
            if (inventory == null)
                return;
        }

        IsOpen = true;
        frame.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
    }

    void Close()
    {
        if (frame == null)
            return;

        IsOpen = false;
        frame.SetActive(false);
        Time.timeScale = 1f;
    }

    void HandleDragStarted(InventorySlotUI source)
    {
        if (dragGhost == null)
            return;
        dragGhost.sprite = source.GetIcon();
        dragGhost.enabled = true;
        dragGhost.gameObject.SetActive(true);
    }

    void HandleDragEnded()
    {
        if (dragGhost == null)
            return;
        dragGhost.gameObject.SetActive(false);
    }

    void HandleSlotDrop(InventorySlotUI source, InventorySlotUI target)
    {
        if (inventory == null)
            return;

        var srcItem = GetItemAt(source.slotType, source.slotIndex);
        if (srcItem == null)
            return;

        if (target.slotType == SlotType.Weapon && srcItem is WeaponData weapon)
        {
            RemoveItemFrom(source.slotType, source.slotIndex);
            var old = inventory.EquipWeapon(target.slotIndex, weapon);
            if (old != null)
                PutItemInto(source.slotType, source.slotIndex, old);
        }
        else if (target.slotType == SlotType.Accessory && srcItem is AccessoryData accessory)
        {
            RemoveItemFrom(source.slotType, source.slotIndex);
            var old = inventory.EquipAccessory(target.slotIndex, accessory);
            if (old != null)
                PutItemInto(source.slotType, source.slotIndex, old);
        }
        else if (target.slotType == SlotType.Backpack)
        {
            var targetItem = GetItemAt(SlotType.Backpack, target.slotIndex);

            if (source.slotType == SlotType.Weapon)
            {
                if (targetItem != null && !(targetItem is WeaponData))
                    return;
                RemoveItemFrom(SlotType.Backpack, target.slotIndex);
                var old = inventory.UnequipWeapon(source.slotIndex);
                PutItemInto(SlotType.Backpack, target.slotIndex, old);
                if (targetItem is WeaponData tw)
                    inventory.EquipWeapon(source.slotIndex, tw);
            }
            else if (source.slotType == SlotType.Accessory)
            {
                if (targetItem != null && !(targetItem is AccessoryData))
                    return;
                RemoveItemFrom(SlotType.Backpack, target.slotIndex);
                var old = inventory.UnequipAccessory(source.slotIndex);
                PutItemInto(SlotType.Backpack, target.slotIndex, old);
                if (targetItem is AccessoryData ta)
                    inventory.EquipAccessory(source.slotIndex, ta);
            }
            else if (source.slotType == SlotType.Backpack)
            {
                SwapBackpack(source.slotIndex, target.slotIndex);
            }
        }
        else if (source.slotType == target.slotType && source.slotType != SlotType.Backpack)
        {
            SwapEquipSlots(source, target);
        }

        Refresh();
    }

    ScriptableObject GetItemAt(SlotType type, int index)
    {
        switch (type)
        {
            case SlotType.Weapon:
                var weapons = inventory.WeaponInventory;
                if (weapons != null && index < weapons.weapons.Count)
                    return weapons.weapons[index];
                return null;
            case SlotType.Accessory:
                if (index < inventory.Accessories.Count)
                    return inventory.Accessories[index];
                return null;
            case SlotType.Backpack:
                if (index < inventory.Backpack.Count)
                    return inventory.Backpack[index];
                return null;
        }
        return null;
    }

    void RemoveItemFrom(SlotType type, int index)
    {
        switch (type)
        {
            case SlotType.Weapon:
                inventory.UnequipWeapon(index);
                break;
            case SlotType.Accessory:
                inventory.UnequipAccessory(index);
                break;
            case SlotType.Backpack:
                inventory.RemoveFromBackpack(index);
                break;
        }
    }

    void PutItemInto(SlotType type, int index, ScriptableObject item)
    {
        if (item == null)
            return;
        switch (type)
        {
            case SlotType.Weapon:
                if (item is WeaponData w)
                    inventory.EquipWeapon(index, w);
                break;
            case SlotType.Accessory:
                if (item is AccessoryData a)
                    inventory.EquipAccessory(index, a);
                break;
            case SlotType.Backpack:
                inventory.AddToBackpack(item);
                break;
        }
    }

    void SwapBackpack(int indexA, int indexB)
    {
        var bp = inventory.Backpack;
        var a = indexA < bp.Count ? bp[indexA] : null;
        var b = indexB < bp.Count ? bp[indexB] : null;
        if (a != null)
            inventory.RemoveFromBackpack(indexA);
        if (b != null)
        {
            int bIdx = indexB > indexA && a != null ? indexB - 1 : indexB;
            inventory.RemoveFromBackpack(bIdx);
        }
        if (b != null)
            InsertBackpack(indexA, b);
        if (a != null)
            InsertBackpack(indexB, a);
    }

    void InsertBackpack(int index, ScriptableObject item)
    {
        inventory.InsertToBackpack(index, item);
    }

    void SwapEquipSlots(InventorySlotUI a, InventorySlotUI b)
    {
        if (a.slotType == SlotType.Weapon)
        {
            var wa = inventory.UnequipWeapon(a.slotIndex);
            var wb = inventory.UnequipWeapon(b.slotIndex);
            if (wa != null)
                inventory.EquipWeapon(b.slotIndex, wa);
            if (wb != null)
                inventory.EquipWeapon(a.slotIndex, wb);
        }
        else if (a.slotType == SlotType.Accessory)
        {
            var aa = inventory.UnequipAccessory(a.slotIndex);
            var ab = inventory.UnequipAccessory(b.slotIndex);
            if (aa != null)
                inventory.EquipAccessory(b.slotIndex, aa);
            if (ab != null)
                inventory.EquipAccessory(a.slotIndex, ab);
        }
    }

    void Refresh()
    {
        if (inventory == null)
            return;

        var weapons = inventory.WeaponInventory;
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weaponSlots[i] == null)
                continue;
            bool hasItem =
                weapons != null && i < weapons.weapons.Count && weapons.weapons[i] != null;
            if (hasItem)
                weaponSlots[i].SetItem(weapons.weapons[i].sprite);
            else
                weaponSlots[i].Clear();
        }

        for (int i = 0; i < accessorySlots.Count; i++)
        {
            if (accessorySlots[i] == null)
                continue;
            bool hasItem = i < inventory.Accessories.Count && inventory.Accessories[i] != null;
            if (hasItem)
                accessorySlots[i].SetItem(inventory.Accessories[i].icon);
            else
                accessorySlots[i].Clear();
        }

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            if (backpackSlots[i] == null)
                continue;
            if (i < inventory.Backpack.Count && inventory.Backpack[i] != null)
            {
                var item = inventory.Backpack[i];
                if (item is WeaponData w)
                    backpackSlots[i].SetItem(w.sprite);
                else if (item is AccessoryData a)
                    backpackSlots[i].SetItem(a.icon);
                else
                    backpackSlots[i].Clear();
            }
            else
            {
                backpackSlots[i].Clear();
            }
        }

        if (goldText != null)
            goldText.text = inventory.Gold.ToString();
    }
}
