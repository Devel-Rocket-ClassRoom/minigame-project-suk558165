using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI countdownText;

    [Header("Settings")]
    public float reloadDelay = 3f;
    public float fadeInDuration = 0.6f;

    private PlayerHealth playerHealth;
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

    void Update()
    {
        if (triggered)
            return;

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null || !playerHealth.IsDead)
            return;

        triggered = true;
        StartCoroutine(ShowAndReload());
    }

    IEnumerator ShowAndReload()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        float remaining = reloadDelay;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}초 후 재시작";
            remaining -= Time.deltaTime;
            yield return null;
        }

        GameFlowController.Instance?.GoToTitle();
    }

    public void ResetUI()
    {
        triggered = false;
        playerHealth = null;
        StopAllCoroutines();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
