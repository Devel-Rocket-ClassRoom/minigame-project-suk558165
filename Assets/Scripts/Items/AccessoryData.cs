using UnityEngine;

[CreateAssetMenu(menuName = "Game/AccessoryData", fileName = "NewAccessory")]
public class AccessoryData : ScriptableObject
{
    public string accessoryName;
    public Sprite icon;
    [TextArea]
    public string description;
    public int price;

    [Header("Stat Bonuses")]
    public float maxHpBonus;
    public float damageBonus;
    public float speedBonus;
}
