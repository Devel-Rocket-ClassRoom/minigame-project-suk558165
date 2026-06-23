using UnityEngine;

/// <summary>
/// 도전과제 조건의 종류.
/// AchievementManager가 SaveData의 어떤 값을 기준으로 달성을 판단할지 결정한다.
/// </summary>
public enum AchievementTrigger
{
    // ── 누적 통계 (SaveData.totelData 기준) ──
    TotalKills,           // 총 처치 수
    TotalBossKills,       // 총 보스 처치 수
    TotalDeaths,          // 총 사망 수
    TotalRuns,            // 총 런(게임) 횟수
    TotalGold,            // 누적 골드 획득량
    TotalPlayTime,        // 누적 플레이타임 (초)

    // ── 한 런의 베스트 기록 (SaveData.bestRun 기준) ──
    BestKillsInRun,       // 한 판 최다 처치
    BestGoldInRun,        // 한 판 최다 골드
    BestPlayTimeInRun,    // 한 판 최장 생존 시간 (초)
    BestDamageInRun,      // 한 판 최다 데미지

    // ── 특수 트리거 ──
    /// <summary>모든 상점 아이템(무기+악세서리)을 한 번 이상 구매했을 때 달성.</summary>
    AllItemsPurchased,

    /// <summary>코드에서 직접 잠금 해제를 호출해야 달성 (예: 노피격 클리어).</summary>
    Manual,
}

/// <summary>
/// 단일 도전과제 정의. ScriptableObject라서 .asset 파일로 만들어
/// AchievementDatabase의 리스트에 등록한다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Achievement", fileName = "NewAchievement")]
public class AchievementData : ScriptableObject
{
    [Tooltip("세이브에 저장되는 고유 식별자. 한 번 정하면 절대 변경 금지 (변경 시 기존 달성 기록과 매칭 안 됨)")]
    public string id;

    [Tooltip("UI에 표시될 도전과제 이름")]
    public string title;

    [TextArea]
    [Tooltip("UI에 표시될 설명")]
    public string description;

    [Tooltip("달성 후 아이콘 (없으면 표시 안 함)")]
    public Sprite icon;

    [Tooltip("미달성 시 아이콘 (자물쇠 그림 등). 비워두면 icon을 흐리게 사용.")]
    public Sprite lockedIcon;

    [Tooltip("달성 전까지 목록에서 숨김 (스팀의 '숨김 도전과제' 같은 효과)")]
    public bool hidden;

    [Tooltip("어떤 조건으로 평가할지")]
    public AchievementTrigger trigger;

    [Tooltip("조건 임계값 (예: TotalKills=100, TotalPlayTime=3600000초). AllItemsPurchased/Manual은 사용 안 함.")]
    public float threshold;
}
