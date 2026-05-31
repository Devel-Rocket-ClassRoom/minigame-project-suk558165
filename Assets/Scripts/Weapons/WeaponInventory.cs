using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    public const int MaxSlots = 2;

    public List<WeaponData> weapons = new List<WeaponData>();
    public int currentIndex = 0;

    public WeaponData Current => weapons.Count > 0 ? weapons[currentIndex] : null;

    public event System.Action<WeaponData> OnWeaponChanged;

    void Update()
    {
        if (weapons.Count < 2 || InventoryUI.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            currentIndex = (currentIndex + 1) % weapons.Count;
            OnWeaponChanged?.Invoke(Current);
        }
    }
}
