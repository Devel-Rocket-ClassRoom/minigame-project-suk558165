using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        float remaining = returnDelay;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}초 후 타이틀로 복귀";
            remaining -= Time.deltaTime;
            yield return null;
        }

        GameFlowController.Instance?.GoToTitle();
    }

    public void ResetUI()
    {
        triggered = false;
        StopAllCoroutines();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
