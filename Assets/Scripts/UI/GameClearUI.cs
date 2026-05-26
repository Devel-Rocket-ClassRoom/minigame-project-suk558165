using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClearUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI countdownText;

    [Header("Settings")]
    public float returnDelay = 5f;
    public float fadeInDuration = 0.8f;

    private bool triggered;

    void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show()
    {
        if (triggered)
            return;

        triggered = true;
        StartCoroutine(ShowAndReturn());
    }

    IEnumerator ShowAndReturn()
    {
        // 페이드 인
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        // 카운트다운 후 타이틀로 복귀
        float remaining = returnDelay;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}초 후 타이틀로 복귀";
            remaining -= Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
