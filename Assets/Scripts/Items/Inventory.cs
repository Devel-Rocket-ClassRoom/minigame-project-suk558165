using System;
using System.Collections.Generic;
using UnityEngine;

public struct StatBonus
{
    public float maxHp;
    public float damage;
    public float speed;
    public float jump;

    public float criticalChance;
    public float criticalDamage;
    public float attackSpeed;
    public float damageReduction;

    public float damageReceivedMult;
    public float damageDealtMult;

    public int dashCount;
    public float dashRange;

    public float evasionRate;
    public float goldDrop;

    public int arrowCount;
    public float arrowDamageMult;
    public int penetration;
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

    public static Inventory Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        if (weaponInventory == null)
            weaponInventory = GetComponent<WeaponInventory>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool AddToBackpack(ScriptableObject item)
    {
        if (backpack.Count >= MaxBackpack)
            return false;
        backpack.Add(item);
        RunStats.Instance?.AddItem();
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

        if (slotIndex >= WeaponInventory.MaxSlots)
            return weapon;

        while (weaponInventory.weapons.Count <= slotIndex)
            weaponInventory.weapons.Add(null);

        var old = weaponInventory.weapons[slotIndex];
        weaponInventory.weapons[slotIndex] = weapon;
        OnInventoryChanged?.Invoke();
        SaveEquippedWeapons();
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
        SaveEquippedWeapons();
        return old;
    }

    void SaveEquippedWeapons()
    {
        if (SaveManager.Instance == null || weaponInventory == null)
            return;

        var names = SaveManager.Instance.Data.equippedWeapons;
        names.Clear();
        foreach (var w in weaponInventory.weapons)
            names.Add(w != null ? w.id : "");
        SaveManager.Instance.Save();
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
        RunStats.Instance?.AddGold(amount);
        OnInventoryChanged?.Invoke();
        SaveGold();
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;
        gold -= amount;
        OnInventoryChanged?.Invoke();
        SaveGold();
        return true;
    }

    void SaveGold()
    {
        if (SaveManager.Instance == null)
            return;
        SaveManager.Instance.Data.gold = gold;
        SaveManager.Instance.Save();
    }

    public void LoadGold()
    {
        if (SaveManager.Instance != null)
            gold = SaveManager.Instance.Data.gold;
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
            bonus.jump += acc.jumpBonus;
            bonus.criticalChance += acc.criticalChance;
            bonus.criticalDamage += acc.criticalDamage;
            bonus.attackSpeed += acc.attackSpeedBonus;
            bonus.damageReduction += acc.damageReduction;
            bonus.damageReceivedMult += acc.damageReceivedMult;
            bonus.damageDealtMult += acc.damageDealtMult;
            bonus.dashCount += acc.dashCountBonus;
            bonus.dashRange += acc.dashRangeBonus;
            bonus.evasionRate += acc.evasionRate;
            bonus.goldDrop += acc.goldDropBonus;
            bonus.arrowCount += acc.arrowCount;
            bonus.arrowDamageMult += acc.arrowDamageMult;
            bonus.penetration += acc.penetrationCount;
        }
        return bonus;
    }
}
