using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenHitEffect : MonoBehaviour
{
    public static ScreenHitEffect Instance { get; private set; }

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
    private Coroutine flashCoroutine;

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
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        overlay.color = new Color(1f, 0f, 0f, flashAlpha);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(flashAlpha, 0f, elapsed / fadeDuration);
            overlay.color = new Color(1f, 0f, 0f, a);
            yield return null;
        }
        overlay.color = new Color(1f, 0f, 0f, 0f);
    }
}
