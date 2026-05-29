using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = SaveManager.CurrentVersion;

    // 재화
    public int gold;

    // 기본 장착 무기 (이름으로 저장)
    public List<string> equippedWeapons = new List<string>();

    // 해금 목록 (ScriptableObject 이름으로 저장)
    public List<string> unlockedWeapons = new List<string>();
    public List<string> unlockedAccessories = new List<string>();

    // 베스트 런 기록
    public BestRunData bestRun = new BestRunData();

    // 오디오 설정
    public float volumeMaster = 1f;
    public float volumeBGM = 1f;
    public float volumeSFX = 1f;
}

[Serializable]
public class BestRunData
{
    public int bestKills;
    public int bestGoldEarned;
    public float bestPlayTime;
    public float bestDamageDealt;
}
