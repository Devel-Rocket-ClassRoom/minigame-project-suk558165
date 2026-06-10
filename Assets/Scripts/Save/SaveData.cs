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

    // 마을 영구 강화 레벨 (인덱스 = MetaUpgradeType)
    public List<int> permaUpgradeLevels = new List<int>();

    // 베스트 런 기록
    public BestRunData bestRun = new BestRunData();

    // 오디오 설정
    public float volumeMaster = 1f;
    public float volumeBGM = 1f;
    public float volumeSFX = 1f;

    // 마지막 위치
    public string lastLocation = "Village"; // "Village" or "Dungeon"
    public int lastRoomNumber = 1;

    // 던전 방 배치
    public List<int> savedRoomOrder = new List<int>();
    public int savedRoomCursor = 0;

    // 언어 설정 ("ko" or "en", 빈 문자열이면 시스템 기본)
    public string languageCode = "";

    // 튜토리얼
    public bool tutorialCompleted = false;

    // 키 바인딩
    public KeyBindingSaveData keyBindings = new KeyBindingSaveData();

    // 디스플레이 설정 (0 이하 = 미설정, 시스템 기본 사용)
    public int resolutionWidth = 0;
    public int resolutionHeight = 0;
    public int refreshRate = 0;
    public int fullscreenMode = -1; // -1 = 미설정, 0=Windowed, 1=Borderless, 2=Exclusive Fullscreen
}

[Serializable]
public class KeyBindingSaveData
{
    public string dashKey = "Z";
    public string attackKey = "X";
    public string inventoryKey = "Tab";
    public string interactKey = "A";
}

[Serializable]
public class BestRunData
{
    public int bestKills;
    public int bestGoldEarned;
    public float bestPlayTime;
    public float bestDamageDealt;
}
