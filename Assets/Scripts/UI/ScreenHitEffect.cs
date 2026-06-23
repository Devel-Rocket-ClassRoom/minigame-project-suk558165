using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenHitEffect : MonoBehaviour
{
    public static ScreenHitEffect Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [SerializeField]
    float flashAlpha = 0.4f;

    [SerializeField]
    float fadeDuration = 0.35f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null)
            return;
        var go = new GameObject("ScreenHitEffect");
        go.AddComponent<ScreenHitEffect>();
    }

    private Image overlay;
    private CancellationTokenSource _flashCts;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
    }

    void CreateOverlay()
    {
        var canvasGO = new GameObject("HitEffectCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("HitOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        overlay = imgGO.AddComponent<Image>();
        overlay.color = new Color(1f, 0f, 0f, 0f);
        overlay.raycastTarget = false;

        var rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void Flash()
    {
        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = new CancellationTokenSource();
        FlashRoutine(_flashCts.Token).Forget();
    }

    async UniTaskVoid FlashRoutine(CancellationToken token)
    {
        overlay.color = new Color(1f, 0f, 0f, flashAlpha);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // Time.timeScale=0(게임오버 등)에서도 페이드아웃 진행되도록 unscaled 사용
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(flashAlpha, 0f, elapsed / fadeDuration);
            overlay.color = new Color(1f, 0f, 0f, a);
            await UniTask.Yield(token);
        }
        overlay.color = new Color(1f, 0f, 0f, 0f);
    }

    /// <summary>오버레이 즉시 초기화. 게임오버/씬 전환 시 호출.</summary>
    public void Clear()
    {
        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = null;
        if (overlay != null)
            overlay.color = new Color(1f, 0f, 0f, 0f);
    }
}
