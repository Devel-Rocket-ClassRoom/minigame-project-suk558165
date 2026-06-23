using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도전과제 목록 패널. NPC와 상호작용해서 열고 닫는다.
///
/// UpgradeShopUI와 같은 패턴:
///  - IsOpen 정적 프로퍼티로 열림 상태 추적
///  - Open() 시 Time.timeScale = 0 (게임 정지)
///  - Close() 시 Time.timeScale = 1 (재개)
///  - openCount로 여러 패널 동시 열림 케이스 처리
///
/// NpcController가 이 컴포넌트를 슬롯에 받아서 자동으로 토글한다.
/// </summary>
public class AchievementListUI : MonoBehaviour
{
    [SerializeField]
    [Tooltip("실제로 보이는/숨겨지는 패널 (보통 Frame 자식)")]
    GameObject frame;

    [SerializeField]
    [Tooltip("도전과제 항목들이 인스턴스화되는 부모. 보통 ScrollView의 Content 또는 VerticalLayoutGroup")]
    Transform listParent;

    [SerializeField]
    [Tooltip("항목 한 줄 프리팹 (Icon/Title/Description 자식 필수)")]
    GameObject entryPrefab;

    [SerializeField]
    [Tooltip("'달성: X / Y' 형태 요약 텍스트 (선택)")]
    TextMeshProUGUI summaryText;

    [SerializeField]
    [Tooltip("닫기 버튼 (선택). 클릭 시 Close() 호출")]
    Button closeButton;

    // ── 다른 코드(NpcController 등)에서 열림 여부를 확인할 수 있도록 정적 프로퍼티 노출 ──
    public static bool IsOpen => openCount > 0;
    public static bool JustClosed => Time.frameCount == closedFrame;
    private static int openCount = 0;
    private static int closedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        openCount = 0;
        closedFrame = -1;
    }

    void Awake()
    {
        closeButton?.onClick.AddListener(Close);
    }

    void Start()
    {
        // 처음엔 닫힌 상태로 시작 (다른 패널이 IsOpen=true면 유지)
        if (!IsOpen)
            frame?.SetActive(false);
    }

    /// <summary>패널을 열고 목록을 갱신. 게임을 정지시킨다.</summary>
    public void Open()
    {
        if (frame == null) return;
        Refresh();
        frame.SetActive(true);
        openCount++;
        Time.timeScale = 0f;
    }

    /// <summary>패널을 닫고 게임을 재개. (openCount가 0이 되었을 때만 재개)</summary>
    public void Close()
    {
        if (frame == null || !frame.activeSelf) return;
        frame.SetActive(false);
        openCount = Mathf.Max(0, openCount - 1);
        closedFrame = Time.frameCount;
        if (openCount == 0)
            Time.timeScale = 1f;
    }

    /// <summary>도전과제 목록을 다시 그린다 (열 때마다 자동 호출).</summary>
    public void Refresh()
    {
        if (AchievementManager.Instance == null || AchievementManager.Instance.Database == null)
            return;
        if (listParent == null || entryPrefab == null) return;

        // 기존 항목 제거
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        var db = AchievementManager.Instance.Database;
        int unlockedCount = 0;
        int totalShown = 0;

        foreach (var ach in db.achievements)
        {
            if (ach == null) continue;
            bool unlocked = AchievementManager.Instance.IsUnlocked(ach.id);

            // 숨김 도전과제는 달성 전엔 목록에서 보이지 않음
            if (ach.hidden && !unlocked) continue;

            // entryPrefab을 listParent 아래 인스턴스화하고 자식 컴포넌트 채우기
            var entry = Instantiate(entryPrefab, listParent);
            var icon = entry.transform.Find("Icon")?.GetComponent<Image>();
            var title = entry.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var desc = entry.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();

            if (icon != null) icon.sprite = unlocked ? ach.icon : (ach.lockedIcon ?? ach.icon);
            if (title != null) title.text = ach.title;
            if (desc != null) desc.text = ach.description;

            // 미달성 항목은 흐리게
            var cg = entry.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = unlocked ? 1f : 0.45f;

            if (unlocked) unlockedCount++;
            totalShown++;
        }

        if (summaryText != null)
            summaryText.text = $"달성: {unlockedCount} / {totalShown}";
    }
}
