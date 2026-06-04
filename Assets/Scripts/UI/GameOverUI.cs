using System.Collections;
using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
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

    [Header("Audio")]
    public AudioClip gameOverSound;

    [Header("Return")]
    public TextMeshProUGUI returnHintText;
    public KeyCode returnKey = KeyCode.X;

    private bool triggered;
    private bool canReturn;

    public static GameOverUI Instance { get; private set; }

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

    void Update()
    {
        if (!triggered)
        {
            var ph = PlayerRef.Health;
            if (ph != null && ph.IsDead)
            {
                triggered = true;
                RunStats.Instance?.StopTimer();
                AudioManager.Instance?.PlaySFX(gameOverSound);
                Time.timeScale = 0f;
                StartCoroutine(ShowRoutine());
            }
            return;
        }

        if (canReturn && Input.GetKeyDown(returnKey))
            ReturnToVillage();
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

        if (returnHintText != null)
            returnHintText.text = $"[ {returnKey} ] 마을로 돌아가기";

        canReturn = true;
    }

    void PopulateStats()
    {
        var s = RunStats.Instance;
        if (s == null)
            return;

        int sec = Mathf.FloorToInt(s.PlayTime);
        SetText(playTimeText, $"플레이 타임  {sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2}");
        SetText(deathCountText, $"사망 횟수  {s.Deaths}");
        SetText(killCountText, $"처치 수  {s.Kills}");
        SetText(goldEarnedText, $"획득 골드  {s.GoldEarned}");
        SetText(damageDealtText, $"총 딜량  {Mathf.RoundToInt(s.DamageDealt)}");
        SetText(damageTakenText, $"받은 피해  {Mathf.RoundToInt(s.DamageTaken)}");
        SetText(itemsGainedText, $"획득 아이템  {s.ItemsGained}");
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
