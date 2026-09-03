using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    public const int MaxSlots = 2;

    public List<WeaponData> weapons = new List<WeaponData>();
    public int currentIndex = 0;

    public WeaponData Current =>
        weapons.Count > 0 && currentIndex < weapons.Count ? weapons[currentIndex] : null;

    public event System.Action<WeaponData> OnWeaponChanged;

    private List<WeaponData> defaultWeapons;

    void Awake()
    {
        defaultWeapons = new List<WeaponData>(weapons);
    }

    public void ResetToDefault()
    {
        weapons = new List<WeaponData>(defaultWeapons);
        currentIndex = 0;
        NotifyWeaponChanged();
    }

    void Update()
    {
        if (weapons.Count < 2)
            return;

        // PlayerCombat 과 동일한 가드. 이게 없으면 대화·상점·일시정지 중에도 무기가 바뀐다.
        if (InventoryUI.IsOpen || ShopUI.IsOpen || PauseMenu.IsPaused || DialogueUI.IsOpen)
            return;

        var switchKey = InputManager.Instance?.WeaponSwitch ?? KeyCode.C;
        if (Input.GetKeyDown(switchKey))
        {
            currentIndex = (currentIndex + 1) % weapons.Count;
            NotifyWeaponChanged();
        }
    }

    /// <summary>현재 활성 무기가 바뀌었음을 구독자에게 알림.</summary>
    public void NotifyWeaponChanged()
    {
        OnWeaponChanged?.Invoke(Current);
    }
}
