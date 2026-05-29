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

    [Header("기본 스탯")]
    public float maxHpBonus;
    public float damageBonus;
    public float speedBonus;
    public float jumpBonus;

    [Header("전투")]
    public float criticalChance;
    public float criticalDamage;
    public float attackSpeedBonus;
    public float damageReduction;

    [Header("리스크/리워드")]
    public float damageReceivedMult;
    public float damageDealtMult;

    [Header("대쉬")]
    public int dashCountBonus;
    public float dashRangeBonus;

    [Header("기타")]
    public float evasionRate;
    public float goldDropBonus;

    [Header("투사체 (활 장착 시)")]
    public int arrowCount;
    public float arrowDamageMult;
    public int penetrationCount;
}
