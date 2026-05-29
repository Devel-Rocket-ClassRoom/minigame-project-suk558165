using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Game/AccessoryData", fileName = "NewAccessory")]
public class AccessoryData : ScriptableObject
{
    [Tooltip("세이브/로드에 사용되는 고유 식별자. 절대 변경 금지.")]
    public string id;

    public LocalizedString accessoryName;
    public Sprite icon;
    public LocalizedString description;
    public int price;

    [Header("Stat Bonuses")]
    public float maxHpBonus;
    public float damageBonus;
    public float speedBonus;
}
