using UnityEngine;

/// <summary>
/// 마을 영구 강화의 런타임 로직.
/// 레벨은 SaveData.permaUpgradeLevels 에 저장되며, 재화는 기존 골드를 사용한다.
/// 강화 정의(비용·효과)는 MetaUpgradeConfig 에셋에서 읽는다.
/// </summary>
public static class MetaUpgrades
{
    // 이번 런에서 부활을 이미 사용했는지 (저장하지 않음 — 마을/던전 재진입 시 리셋)
    private static bool reviveUsedThisRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => reviveUsedThisRun = false;

    static int TypeCount => System.Enum.GetValues(typeof(MetaUpgradeType)).Length;

    // ── 레벨 조회 / 저장 ──────────────────────────────────

    public static int GetLevel(MetaUpgradeType type)
    {
        var data = SaveManager.Instance?.Data;
        if (data == null)
            return 0;
        var levels = data.permaUpgradeLevels;
        int i = (int)type;
        return (i >= 0 && i < levels.Count) ? levels[i] : 0;
    }

    static void SetLevel(MetaUpgradeType type, int level)
    {
        var data = SaveManager.Instance?.Data;
        if (data == null)
            return;
        var levels = data.permaUpgradeLevels;
        while (levels.Count < TypeCount)
            levels.Add(0);
        levels[(int)type] = level;
        SaveManager.Instance.Save();
    }

    public static int GetMaxLevel(MetaUpgradeType type) =>
        MetaUpgradeConfig.Instance?.Find(type)?.maxLevel ?? 0;

    public static bool IsMaxed(MetaUpgradeType type) =>
        GetLevel(type) >= GetMaxLevel(type);

    /// <summary>다음 레벨 구매 비용. 최대 레벨이면 -1.</summary>
    public static int GetNextCost(MetaUpgradeType type)
    {
        var entry = MetaUpgradeConfig.Instance?.Find(type);
        if (entry == null)
            return -1;
        int level = GetLevel(type);
        if (level >= entry.maxLevel)
            return -1;
        return entry.baseCost + level * entry.costStep;
    }

    /// <summary>골드를 차감하고 한 단계 강화한다. 성공 시 true.</summary>
    public static bool TryUpgrade(MetaUpgradeType type)
    {
        int cost = GetNextCost(type);
        if (cost < 0)
            return false;

        var inventory = Inventory.Instance;
        if (inventory == null || inventory.Gold < cost)
            return false;

        if (!inventory.SpendGold(cost))
            return false;

        SetLevel(type, GetLevel(type) + 1);
        return true;
    }

    // ── 효과 산출 ─────────────────────────────────────────

    static float ValuePerLevel(MetaUpgradeType type) =>
        MetaUpgradeConfig.Instance?.Find(type)?.valuePerLevel ?? 0f;

    /// <summary>스탯 보너스에 영구 강화분을 더한다. GetTotalStatBonus 끝에서 호출.</summary>
    public static void Contribute(ref StatBonus bonus)
    {
        bonus.maxHp += GetLevel(MetaUpgradeType.MaxHp) * ValuePerLevel(MetaUpgradeType.MaxHp);
        bonus.damage += GetLevel(MetaUpgradeType.Damage) * ValuePerLevel(MetaUpgradeType.Damage);
        bonus.criticalChance += GetLevel(MetaUpgradeType.CritChance) * ValuePerLevel(MetaUpgradeType.CritChance);
        bonus.speed += GetLevel(MetaUpgradeType.MoveSpeed) * ValuePerLevel(MetaUpgradeType.MoveSpeed);
        bonus.goldDrop += GetLevel(MetaUpgradeType.GoldGain) * ValuePerLevel(MetaUpgradeType.GoldGain);
    }

    /// <summary>포션 드랍 확률 가산치 (0~1).</summary>
    public static float PotionDropBonus =>
        GetLevel(MetaUpgradeType.PotionDrop) * ValuePerLevel(MetaUpgradeType.PotionDrop);

    /// <summary>보스/미니보스에게 주는 피해 배율 (1 + 보너스).</summary>
    public static float BossDamageMult =>
        1f + GetLevel(MetaUpgradeType.BossDamage) * ValuePerLevel(MetaUpgradeType.BossDamage);

    // ── 1회성 부활 ────────────────────────────────────────

    public static bool CanRevive =>
        GetLevel(MetaUpgradeType.Revive) > 0 && !reviveUsedThisRun;

    public static void ConsumeRevive() => reviveUsedThisRun = true;

    /// <summary>새 런 시작 시 호출 — 부활 사용 플래그 리셋.</summary>
    public static void BeginRun() => reviveUsedThisRun = false;
}
