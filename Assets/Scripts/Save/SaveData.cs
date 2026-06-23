using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = SaveManager.CurrentVersion;

    // 클라우드 동기화용 타임스탬프 (Unix millis)
    public long updatedAt;

    // 재화
    public int gold;

    // 기본 장착 무기 (id로 저장)
    public List<string> equippedWeapons = new List<string>();

    // 장착 악세서리 (id로 저장, 빈 슬롯은 "")
    public List<string> equippedAccessories = new List<string>();

    // 가방 아이템 (순서 보존, "w:id" 또는 "a:id" 형식)
    public List<string> backpackItems = new List<string>();

    // (구버전 호환) 무기와 악세서리를 따로 저장하던 필드
    public List<string> backpackWeapons = new List<string>();
    public List<string> backpackAccessories = new List<string>();

    // 마을 영구 강화 레벨 (인덱스 = MetaUpgradeType)
    public List<int> permaUpgradeLevels = new List<int>();

    // 베스트 런 기록
    public BestRunData bestRun = new BestRunData();

    public TotelData totelData = new TotelData();

    // 달성한 도전과제 id 목록
    public List<string> unlockedAchievements = new List<string>();

    // 한 번이라도 구매한 아이템 id 목록 (도전과제용)
    public List<string> purchasedItemIds = new List<string>();

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
    public string jumpKey = "Space";
    public string dashKey = "Z";
    public string attackKey = "X";
    public string inventoryKey = "Tab";
    public string interactKey = "A";
    public string weaponSwitchKey = "C";
}

[Serializable]
public class BestRunData
{
    public int bestKills;
    public int bestGoldEarned;
    public float bestPlayTime;
    public float bestDamageDealt;
}

[Serializable]
public class TotelData
{
    public int totalKills;

    public int totalDeaths;

    public int totalRuns;

    public float totalPlaytime;

    // 사람이 읽기 쉬운 형식 "HH:MM:SS" — DB/세이브에 함께 저장 (totalPlaytime 값으로부터 매번 갱신)
    public string totalPlaytimeFormatted = "00:00:00";

    public long totalGoldGet;

    public int totalbossKliis;

    /// <summary>totalPlaytime(초)을 HH:MM:SS 문자열로 변환해 totalPlaytimeFormatted에 동기화.</summary>
    public void UpdatePlaytimeFormatted()
    {
        int sec = (int)totalPlaytime; // 음수 안 나오므로 truncate로 충분
        totalPlaytimeFormatted = $"{sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2}";
    }
}
