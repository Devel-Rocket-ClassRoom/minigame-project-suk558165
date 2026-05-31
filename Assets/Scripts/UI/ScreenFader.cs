using System.Collections;
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

    public Coroutine FadeOut() => StartCoroutine(Fade(0, 1));
    public Coroutine FadeIn() => StartCoroutine(Fade(1, 0));

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        fadeImage.raycastTarget = true;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, to);

        if (to == 0)
            fadeImage.raycastTarget = false;
    }
}
