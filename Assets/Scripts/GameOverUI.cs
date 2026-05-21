using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI countdownText;

    [Header("Settings")]
    public float reloadDelay = 3f;
    public float fadeInDuration = 0.6f;

    private bool triggered;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (triggered || playerHealth == null || !playerHealth.IsDead)
            return;

        triggered = true;
        StartCoroutine(ShowAndReload());
    }

    IEnumerator ShowAndReload()
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

        // 카운트다운
        float remaining = reloadDelay;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}초 후 재시작";
            remaining -= Time.deltaTime;
            yield return null;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
