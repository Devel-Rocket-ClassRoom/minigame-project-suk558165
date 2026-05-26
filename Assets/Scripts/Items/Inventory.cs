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

    [SerializeField] private WeaponInventory weaponInventory;

    private List<AccessoryData> accessories = new List<AccessoryData>();
    private int gold;

    public WeaponInventory WeaponInventory => weaponInventory;
    public IReadOnlyList<AccessoryData> Accessories => accessories;
    public int Gold => gold;

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (weaponInventory == null)
            weaponInventory = GetComponent<WeaponInventory>();
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
            bonus.maxHp += acc.maxHpBonus;
            bonus.damage += acc.damageBonus;
            bonus.speed += acc.speedBonus;
        }
        return bonus;
    }
}
