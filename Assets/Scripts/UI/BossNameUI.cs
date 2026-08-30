using Cysharp.Threading.Tasks;
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

        ShowSequence(displayDuration).Forget();
    }

    async UniTaskVoid ShowSequence(float displayDuration)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;

        await Fade(0f, 1f);

        float holdTime = displayDuration - fadeDuration * 2f;
        if (holdTime > 0f)
            await UniTask.Delay(System.TimeSpan.FromSeconds(holdTime), cancellationToken: this.GetCancellationTokenOnDestroy());

        await Fade(1f, 0f);

        Destroy(gameObject);
    }

    async UniTask Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            await UniTask.Yield();
        }
        canvasGroup.alpha = to;
    }
}
