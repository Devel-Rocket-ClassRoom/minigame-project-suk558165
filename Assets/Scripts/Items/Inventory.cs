using System;
using System.Collections.Generic;
using UnityEngine;

public struct StatBonus
{
    public float maxHp;
    public float damage;
    public float speed;
}

public class Inventory : MonoBehaviour
{
    public const int MaxAccessories = 4;
    public const int MaxBackpack = 8;

    [SerializeField]
    private WeaponInventory weaponInventory;

    private List<AccessoryData> accessories = new List<AccessoryData>();
    private List<ScriptableObject> backpack = new List<ScriptableObject>();
    private int gold;

    public WeaponInventory WeaponInventory => weaponInventory;
    public IReadOnlyList<AccessoryData> Accessories => accessories;
    public IReadOnlyList<ScriptableObject> Backpack => backpack;
    public int Gold => gold;

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (weaponInventory == null)
            weaponInventory = GetComponent<WeaponInventory>();
    }

    public bool AddToBackpack(ScriptableObject item)
    {
        if (backpack.Count >= MaxBackpack)
            return false;
        backpack.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public ScriptableObject RemoveFromBackpack(int index)
    {
        if (index < 0 || index >= backpack.Count)
            return null;
        var item = backpack[index];
        backpack.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return item;
    }

    public WeaponData EquipWeapon(int slotIndex, WeaponData weapon)
    {
        if (weaponInventory == null)
            return weapon;

        while (weaponInventory.weapons.Count <= slotIndex)
            weaponInventory.weapons.Add(null);

        var old = weaponInventory.weapons[slotIndex];
        weaponInventory.weapons[slotIndex] = weapon;
        OnInventoryChanged?.Invoke();
        return old;
    }

    public WeaponData UnequipWeapon(int slotIndex)
    {
        if (weaponInventory == null)
            return null;
        if (slotIndex < 0 || slotIndex >= weaponInventory.weapons.Count)
            return null;

        var old = weaponInventory.weapons[slotIndex];
        weaponInventory.weapons[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        return old;
    }

    public AccessoryData EquipAccessory(int slotIndex, AccessoryData accessory)
    {
        while (accessories.Count <= slotIndex)
            accessories.Add(null);

        var old = accessories[slotIndex];
        accessories[slotIndex] = accessory;
        OnInventoryChanged?.Invoke();
        return old;
    }

    public AccessoryData UnequipAccessory(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= accessories.Count)
            return null;

        var old = accessories[slotIndex];
        accessories[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        return old;
    }

    public bool AddAccessory(AccessoryData data)
    {
        if (accessories.Count >= MaxAccessories)
            return false;
        accessories.Add(data);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveAccessory(int index)
    {
        if (index < 0 || index >= accessories.Count)
            return;
        accessories.RemoveAt(index);
        OnInventoryChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnInventoryChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;
        gold -= amount;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public StatBonus GetTotalStatBonus()
    {
        StatBonus bonus = default;
        foreach (var acc in accessories)
        {
            if (acc == null)
                continue;
            bonus.maxHp += acc.maxHpBonus;
            bonus.damage += acc.damageBonus;
            bonus.speed += acc.speedBonus;
        }
        return bonus;
    }
}
