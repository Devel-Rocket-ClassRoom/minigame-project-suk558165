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

    [Header("추가 효과")]
    [Tooltip("피격 시 받은 데미지의 비율만큼 공격자에게 반사")]
    public float reflectRatio;

    [Tooltip("적에게 가한 데미지의 비율만큼 체력 회복")]
    public float lifesteal;

    [Tooltip("HP가 낮을수록 증가하는 공격력. HP 0일 때 최대 보너스(잃은 체력에 비례)")]
    public float lowHpDamageBonus;

    [Tooltip("대쉬 직후 일정 시간 동안 추가되는 공격 속도")]
    public float dashAttackSpeedBonus;

    [Tooltip("대쉬 공격 속도 버프 지속 시간(초)")]
    public float dashAttackSpeedDuration;

    [Tooltip("발사하는 투사체 속도 배율 증가")]
    public float projectileSpeedMult;

    [Tooltip("물약 회복량 배율 증가")]
    public float potionHealMult;
}
