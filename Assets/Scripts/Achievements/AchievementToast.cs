using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도전과제 달성 시 화면에 잠깐 떴다 사라지는 알림 팝업.
/// 스팀의 도전과제 토스트와 비슷한 느낌.
///
/// 동작:
///  - AchievementManager.OnUnlocked 이벤트 구독
///  - 달성 도전과제를 큐에 쌓아두고 순차 표시
///  - 페이드인 → 표시 → 페이드아웃 → 다음 알림
///
/// 사용:
///  - Canvas 자식으로 배치 (자체 Canvas X)
///  - 슬롯 4개 연결 (canvasGroup, iconImage, titleText, descriptionText)
///  - 초기 알파는 코드가 0으로 만들어둠 (안 보임)
/// </summary>
public class AchievementToast : MonoBehaviour
{
    [SerializeField]
    [Tooltip("페이드 인/아웃에 사용. 비워두면 자기 GameObject에서 자동 탐색.")]
    CanvasGroup canvasGroup;

    [SerializeField]
    [Tooltip("도전과제 아이콘 표시. 없어도 동작 (null 체크 있음)")]
    Image iconImage;

    [SerializeField]
    [Tooltip("'도전과제 달성: XXX' 형태로 표시")]
    TextMeshProUGUI titleText;

    [SerializeField]
    [Tooltip("도전과제 설명 표시")]
    TextMeshProUGUI descriptionText;

    [SerializeField]
    [Tooltip("페이드인 후 유지되는 시간(초)")]
    float displayDuration = 3f;

    [SerializeField]
    [Tooltip("페이드 인/아웃 각각 걸리는 시간(초)")]
    float fadeDuration = 0.4f;

    // 여러 도전과제가 거의 동시에 달성되면 큐에 넣고 차례차례 표시
    Queue<AchievementData> _queue = new Queue<AchievementData>();
    bool _isShowing;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // 시작은 안 보임
    }

    void OnDisable()
    {
        // 부모 캔버스 비활성화 시 코루틴이 중단되면 alpha=1인 상태로 남을 수 있음 → 안전하게 리셋
        StopAllCoroutines();
        _isShowing = false;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    bool _subscribed;

    void Start()
    {
        TrySubscribe();
        if (!_subscribed)
            StartCoroutine(SubscribeWhenReady());
    }

    void TrySubscribe()
    {
        if (_subscribed) return;
        if (AchievementManager.Instance == null) return;
        AchievementManager.Instance.OnUnlocked += OnAchievementUnlocked;
        _subscribed = true;
        Debug.Log("[AchievementToast] subscribed");
    }

    IEnumerator SubscribeWhenReady()
    {
        // AchievementManager.Instance가 아직 없으면 생길 때까지 폴링
        while (!_subscribed)
        {
            yield return null;
            TrySubscribe();
        }
    }

    void OnDestroy()
    {
        if (_subscribed && AchievementManager.Instance != null)
            AchievementManager.Instance.OnUnlocked -= OnAchievementUnlocked;
    }

    /// <summary>도전과제 달성 시 호출됨. 큐에 추가하고 표시 중이 아니면 표시 시작.</summary>
    void OnAchievementUnlocked(AchievementData ach)
    {
        _queue.Enqueue(ach);
        if (!_isShowing)
            StartCoroutine(ShowQueue());
    }

    /// <summary>큐가 빌 때까지 순차적으로 토스트를 표시.</summary>
    IEnumerator ShowQueue()
    {
        _isShowing = true;
        Debug.Log($"[Toast] ShowQueue 시작. canvasGroup null? {canvasGroup == null}, displayDur={displayDuration}, fadeDur={fadeDuration}");
        while (_queue.Count > 0)
        {
            var ach = _queue.Dequeue();
            if (iconImage != null) iconImage.sprite = ach.icon;
            if (titleText != null) titleText.text = $"도전과제 달성: {ach.title}";
            if (descriptionText != null) descriptionText.text = ach.description;

            Debug.Log($"[Toast] 페이드인 시작: {ach.title}");
            yield return Fade(0f, 1f, fadeDuration);
            Debug.Log($"[Toast] 페이드인 끝, {displayDuration}초 대기");
            yield return new WaitForSecondsRealtime(displayDuration);
            Debug.Log($"[Toast] 대기 끝, 페이드아웃 시작");
            yield return Fade(1f, 0f, fadeDuration);
            Debug.Log($"[Toast] 페이드아웃 완료");
        }
        _isShowing = false;
    }

    /// <summary>CanvasGroup의 alpha를 from에서 to로 dur초 동안 보간.</summary>
    IEnumerator Fade(float from, float to, float dur)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            // unscaledDeltaTime: Time.timeScale=0 (일시정지) 상태에서도 페이드 진행
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
