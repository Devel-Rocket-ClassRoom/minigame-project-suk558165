using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    void Awake()
    {
        Instance = this;
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public UniTask FadeOut() => Fade(0, 1);
    public UniTask FadeIn() => Fade(1, 0);

    async UniTask Fade(float from, float to)
    {
        if (fadeImage == null) return;

        fadeImage.raycastTarget = true;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            await UniTask.Yield();
        }
        fadeImage.color = new Color(0, 0, 0, to);

        if (to == 0)
            fadeImage.raycastTarget = false;
    }
}
