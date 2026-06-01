using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossNameUI : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text titleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    public void Show(string bossName, string bossTitle, float displayDuration)
    {
        if (nameText != null)
            nameText.text = bossName;
        if (titleText != null)
            titleText.text = bossTitle;

        StartCoroutine(ShowSequence(displayDuration));
    }

    IEnumerator ShowSequence(float displayDuration)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.alpha = 0f;

        yield return Fade(0f, 1f);

        float holdTime = displayDuration - fadeDuration * 2f;
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        yield return Fade(1f, 0f);

        Destroy(gameObject);
    }

    IEnumerator Fade(float from, float to)
    {
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
