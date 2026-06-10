using UnityEngine;

/// <summary>플레이어 전투 데미지 계산 공용 함수.</summary>
public static class CombatMath
{
    /// <summary>
    /// 저체력 보너스 배율. HP가 낮을수록 최대 (1 + lowHpDamageBonus)까지
    /// 증가하며 잃은 체력에 비례한다. 보너스가 없거나 체력 정보가 없으면 1.
    /// </summary>
    public static float LowHpMultiplier(PlayerHealth health, float lowHpDamageBonus)
    {
        if (lowHpDamageBonus <= 0f || health == null || health.EffectiveMaxHp <= 0f)
            return 1f;
        float hpRatio = Mathf.Clamp01(health.CurrentHp / health.EffectiveMaxHp);
        return 1f + lowHpDamageBonus * (1f - hpRatio);
    }
}
