using System; // Action 델리게이트 사용을 위한 네임스페이스
using UnityEngine; // MonoBehaviour, SerializeField 등 Unity 기본 클래스 사용

/// <summary>
/// 도전과제의 잠금 해제 상태를 관리하는 싱글톤.
///
/// 주요 책임:
///  - Evaluate() 호출 시 모든 도전과제의 조건을 평가해 새로 달성된 것 잠금 해제
///  - OnUnlocked 이벤트로 UI(토스트/목록)에 알림
///  - 달성 시 자동으로 SaveManager.Save() 호출 → 클라우드 동기화까지 자동
///
/// 호출 흐름:
///   RunStats.SaveBestRun() (런 종료 시) → AchievementManager.Evaluate()
///   ShopUI.RecordPurchase() (아이템 구매 시) → AchievementManager.Evaluate()
///   GameClearUI.Show() (노피격 클리어 시) → AchievementManager.UnlockManually("no_hit_clear")
///
/// 상태 저장소:
///   SaveData.unlockedAchievements (List<string>) 를 단일 진실원천(SSOT)으로 사용.
///   별도 캐시를 두지 않으므로 SaveManager가 Data를 교체해도(클라우드 sync 등) 항상 일관성 유지.
/// </summary>
public class AchievementManager : MonoBehaviour // MonoBehaviour를 상속해 씬에 컴포넌트로 배치 가능
{
    public static AchievementManager Instance { get; private set; } // 전역 접근용 싱글톤 인스턴스 (외부 읽기만 허용)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 리로드 시(플레이 진입/종료) 자동 호출
    static void ResetStatics() => Instance = null; // 씬 재진입 시 이전 인스턴스 참조가 남지 않도록 초기화

    [SerializeField] // Inspector에 노출
    [Tooltip("이 게임의 도전과제 데이터베이스 (AchievementDatabase 에셋)")] // Inspector 툴팁
    AchievementDatabase database; // 도전과제 목록을 담은 ScriptableObject 에셋

    /// <summary>도전과제 새로 달성 시 발생 (토스트 UI 등이 구독).</summary>
    public event Action<AchievementData> OnUnlocked; // 달성된 도전과제 데이터를 인자로 전달하는 이벤트

    public AchievementDatabase Database => database; // 외부에서 도전과제 DB에 읽기 전용으로 접근하는 프로퍼티

    void Awake() // 오브젝트가 활성화될 때 최초 1회 호출
    {
        if (Instance != null && Instance != this) // 이미 다른 인스턴스가 존재하면
        {
            Destroy(gameObject); // 중복 오브젝트 즉시 제거
            return; // 이후 초기화 코드 건너뜀
        }
        Instance = this; // 현재 인스턴스를 싱글톤으로 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시에도 오브젝트가 파괴되지 않도록 설정
    }

    public bool IsUnlocked(string id) // 특정 id의 도전과제가 이미 달성됐는지 확인
    {
        var list = SaveManager.Instance?.Data?.unlockedAchievements; // 저장 데이터의 달성 목록을 가져옴 (null-safe)
        return list != null && list.Contains(id); // 목록이 존재하고 해당 id가 포함돼 있으면 true
    }

    /// <summary>
    /// 모든 도전과제 조건을 평가해 새로 달성된 것을 잠금 해제한다.
    /// Manual 트리거는 여기서 평가하지 않고 UnlockManually()로 직접 호출해야 한다.
    /// </summary>
    public void Evaluate() // 런 종료·아이템 구매 등 게임 상태가 바뀔 때마다 호출
    {
        if (database == null || SaveManager.Instance == null) return; // DB 또는 SaveManager가 없으면 즉시 종료
        var data = SaveManager.Instance.Data; // 현재 저장 데이터 참조

        bool anyUnlocked = false; // 이번 호출에서 새로 달성된 것이 있는지 추적
        foreach (var ach in database.achievements) // 등록된 모든 도전과제를 순회
        {
            if (ach == null || string.IsNullOrEmpty(ach.id)) continue; // 유효하지 않은 항목 건너뜀
            if (ach.trigger == AchievementTrigger.Manual) continue; // Manual 트리거는 Evaluate에서 처리하지 않음
            if (data.unlockedAchievements.Contains(ach.id)) continue; // 이미 달성된 항목 건너뜀

            if (Matches(ach, data)) // 조건 충족 여부 확인
            {
                data.unlockedAchievements.Add(ach.id); // 달성 목록에 id 추가
                OnUnlocked?.Invoke(ach); // 달성 이벤트 발생 (토스트 UI 등에 알림)
                anyUnlocked = true; // 새 달성 발생 플래그 설정
            }
        }

        if (anyUnlocked) // 새로 달성된 항목이 하나라도 있으면
            SaveManager.Instance.Save(); // 저장 (클라우드 동기화 포함)
    }

    /// <summary>
    /// 특정 id의 도전과제를 직접 잠금 해제. Manual 트리거용.
    /// 예: GameClearUI에서 노피격 클리어 감지 시 UnlockManually("no_hit_clear") 호출.
    /// </summary>
    /// <returns>새로 잠금 해제됐으면 true, 이미 달성됐거나 실패면 false.</returns>
    public bool UnlockManually(string id) // 외부에서 직접 특정 도전과제를 달성 처리
    {
        if (string.IsNullOrEmpty(id)) return false; // id가 비어있으면 실패 반환
        if (SaveManager.Instance == null || database == null) return false; // 필수 참조 없으면 실패 반환
        var ach = database.Find(id); // DB에서 해당 id의 도전과제 검색
        if (ach == null) return false; // DB에 없는 id면 실패 반환

        var list = SaveManager.Instance.Data.unlockedAchievements; // 달성 목록 참조
        if (list.Contains(id)) return false; // 이미 달성됨

        list.Add(id); // 달성 목록에 추가
        SaveManager.Instance.Save(); // 즉시 저장
        OnUnlocked?.Invoke(ach); // 달성 이벤트 발생
        return true; // 새로 달성됐음을 반환
    }

    bool Matches(AchievementData ach, SaveData data) // 도전과제 조건이 현재 데이터를 충족하는지 판단
    {
        if (ach.trigger == AchievementTrigger.AllItemsPurchased) // '모든 아이템 구매' 조건은 별도 처리
        {
            var db = ItemDatabase.Instance; // 아이템 DB 싱글톤 참조
            if (db == null) return false; // DB 없으면 조건 미충족으로 처리
            int total = db.weapons.Count + db.accessories.Count; // 구매 가능한 전체 아이템 수
            return total > 0 && data.purchasedItemIds.Count >= total; // 전체 아이템을 모두 구매했으면 true
        }
        return GetCurrentValue(ach.trigger, data) >= ach.threshold; // 현재 값이 임계값 이상이면 달성
    }

    float GetCurrentValue(AchievementTrigger t, SaveData d) // 트리거 종류에 맞는 현재 수치를 반환
    {
        switch (t) // 트리거 종류에 따라 분기
        {
            case AchievementTrigger.TotalKills:        return d.totelData.totalKills;        // 누적 총 처치 수
            case AchievementTrigger.TotalBossKills:    return d.totelData.totalbossKliis;    // 누적 보스 처치 수
            case AchievementTrigger.TotalDeaths:       return d.totelData.totalDeaths;       // 누적 사망 횟수
            case AchievementTrigger.TotalRuns:         return d.totelData.totalRuns;         // 누적 런 횟수
            case AchievementTrigger.TotalGold:         return d.totelData.totalGoldGet;      // 누적 획득 골드
            case AchievementTrigger.TotalPlayTime:     return d.totelData.totalPlaytime;     // 누적 플레이 시간(초)
            case AchievementTrigger.BestKillsInRun:    return d.bestRun.bestKills;           // 단일 런 최고 처치 수
            case AchievementTrigger.BestGoldInRun:     return d.bestRun.bestGoldEarned;      // 단일 런 최고 획득 골드
            case AchievementTrigger.BestPlayTimeInRun: return d.bestRun.bestPlayTime;        // 단일 런 최장 플레이 시간
            case AchievementTrigger.BestDamageInRun:   return d.bestRun.bestDamageDealt;     // 단일 런 최고 딜량
            default: return 0; // 알 수 없는 트리거는 0 반환 → 조건 미충족 처리
        }
    }
}
