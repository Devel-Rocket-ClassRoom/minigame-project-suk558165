using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GameClearUI : MonoBehaviour
{
    [Header("Panel")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.6f;

    [Header("Stats")]
    public TextMeshProUGUI playTimeText;
    public TextMeshProUGUI deathCountText;
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI goldEarnedText;
    public TextMeshProUGUI damageDealtText;
    public TextMeshProUGUI damageTakenText;
    public TextMeshProUGUI itemsGainedText;

    [Header("Return")]
    public TextMeshProUGUI returnHintText;
    public KeyCode returnKey = KeyCode.X;

    private bool triggered;
    private bool canReturn;

    public static GameClearUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale _)
    {
        if (triggered)
            PopulateStats();
    }

    public void Show()
    {
        if (triggered)
            return;
        triggered = true;
        RunStats.Instance?.StopTimer();
        Time.timeScale = 0f;

        // 부모 체인이 비활성이면 코루틴 시작이 실패하므로 자기 자신부터 루트까지 활성화.
        for (var t = transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
        }

        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        PopulateStats();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        canReturn = true;

        if (returnHintText != null)
            returnHintText.text = L10n.Format(
                "ui.return_village",
                "[ {0} ] 마을로 돌아가기",
                returnKey
            );
    }

    void Update()
    {
        if (canReturn && Input.GetKeyDown(returnKey))
            ReturnToVillage();
    }

    void PopulateStats()
    {
        var s = RunStats.Instance;
        if (s == null)
            return;

        int sec = Mathf.FloorToInt(s.PlayTime);
        string time = $"{sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2}";
        SetText(playTimeText, L10n.Format("ui.stats.playtime", "플레이 타임  {0}", time));
        SetText(deathCountText, L10n.Format("ui.stats.deaths", "사망 횟수  {0}", s.Deaths));
        SetText(killCountText, L10n.Format("ui.stats.kills", "처치 수  {0}", s.Kills));
        SetText(
            goldEarnedText,
            L10n.Format("ui.stats.gold_earned", "획득 골드  {0}", s.GoldEarned)
        );
        SetText(
            damageDealtText,
            L10n.Format("ui.stats.damage_dealt", "총 딜량  {0}", Mathf.RoundToInt(s.DamageDealt))
        );
        SetText(
            damageTakenText,
            L10n.Format("ui.stats.damage_taken", "받은 피해  {0}", Mathf.RoundToInt(s.DamageTaken))
        );
        SetText(
            itemsGainedText,
            L10n.Format("ui.stats.items_gained", "획득 아이템  {0}", s.ItemsGained)
        );
    }

    void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }

    public void ReturnToVillage()
    {
        Time.timeScale = 1f;
        GameFlowController.Instance?.ReturnToVillage();
    }

    void Hide()
    {
        if (canvasGroup == null)
            return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ResetUI()
    {
        Time.timeScale = 1f;
        triggered = false;
        canReturn = false;
        StopAllCoroutines();
        Hide();
    }
}
