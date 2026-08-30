using System.Threading;
using Cysharp.Threading.Tasks;
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

    private CancellationTokenSource _fadeCts;

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

        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
    }

    public void Show(string message)
    {
        if (messageText != null)
            messageText.text = message;

        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = new CancellationTokenSource();
        Fade(0f, 1f, _fadeCts.Token).Forget();
    }

    public void UpdateText(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public UniTask Hide() => Fade(1f, 0f);

    async UniTask Fade(float from, float to, CancellationToken cancellationToken = default)
    {
        if (canvasGroup == null)
            return;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        canvasGroup.alpha = to;
    }
}
