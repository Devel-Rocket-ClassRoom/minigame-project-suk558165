using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialStepUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI messageText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float fadeDuration = 0.3f;

    public static TutorialStepUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string message)
    {
        if (messageText != null)
            messageText.text = message;

        StopAllCoroutines();
        StartCoroutine(Fade(0f, 1f));
    }

    public void UpdateText(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public Coroutine Hide()
    {
        return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
