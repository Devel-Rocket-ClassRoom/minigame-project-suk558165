using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>마을에서 골드로 구매하는 영구 강화 종류.</summary>
public enum MetaUpgradeType
{
    MaxHp = 0,
    Damage = 1,
    CritChance = 2,
    MoveSpeed = 3,
    GoldGain = 4,
    PotionDrop = 5,
    BossDamage = 6,
    Revive = 7,
}

[Serializable]
public class MetaUpgradeEntry
{
    public MetaUpgradeType type;

    [Tooltip("표시 이름 L10n 키 (비우면 fallbackName 사용)")]
    public string nameKey;

    public string fallbackName;

    [Tooltip("설명 L10n 키 (비우면 fallbackDesc 사용)")]
    public string descKey;

    [TextArea]
    public string fallbackDesc;

    [Tooltip("최대 강화 레벨")]
    public int maxLevel = 5;

    [Tooltip("1레벨 비용")]
    public int baseCost = 100;

    [Tooltip("레벨당 비용 증가량 — cost = baseCost + level * costStep")]
    public int costStep = 100;

    [Tooltip("레벨당 효과값 (체력=고정 가산, 그 외 비율은 0.1 = +10%/레벨)")]
    public float valuePerLevel = 0.1f;
}

[CreateAssetMenu(menuName = "Game/MetaUpgradeConfig", fileName = "MetaUpgradeConfig")]
public class MetaUpgradeConfig : ScriptableObject
{
    public static MetaUpgradeConfig Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    public List<MetaUpgradeEntry> entries = new List<MetaUpgradeEntry>();

    public void Init()
    {
        Instance = this;
    }

    public MetaUpgradeEntry Find(MetaUpgradeType type)
    {
        foreach (var e in entries)
            if (e != null && e.type == type)
                return e;
        return null;
    }
}
