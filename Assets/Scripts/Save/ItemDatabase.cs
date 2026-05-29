using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public static ItemDatabase Instance { get; private set; }

    [Header("등록된 무기 목록")]
    public List<WeaponData> weapons = new List<WeaponData>();

    [Header("등록된 악세서리 목록")]
    public List<AccessoryData> accessories = new List<AccessoryData>();

    public void Init()
    {
        Instance = this;
    }

    public WeaponData FindWeapon(string weaponName)
    {
        foreach (var w in weapons)
            if (w != null && w.weaponName == weaponName)
                return w;
        return null;
    }

    public AccessoryData FindAccessory(string accessoryName)
    {
        foreach (var a in accessories)
            if (a != null && a.accessoryName == accessoryName)
                return a;
        return null;
    }
}
