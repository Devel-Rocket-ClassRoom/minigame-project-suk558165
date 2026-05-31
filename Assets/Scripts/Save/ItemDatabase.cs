using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public static ItemDatabase Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("등록된 무기 목록")]
    public List<WeaponData> weapons = new List<WeaponData>();

    [Header("등록된 악세서리 목록")]
    public List<AccessoryData> accessories = new List<AccessoryData>();

    public void Init()
    {
        Instance = this;
    }

    public WeaponData FindWeapon(string id)
    {
        foreach (var w in weapons)
            if (w != null && w.id == id)
                return w;
        return null;
    }

    public AccessoryData FindAccessory(string id)
    {
        foreach (var a in accessories)
            if (a != null && a.id == id)
                return a;
        return null;
    }
}
