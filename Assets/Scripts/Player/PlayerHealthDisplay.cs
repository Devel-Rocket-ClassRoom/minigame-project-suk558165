using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("Bar Settings")]
    public Vector2 barSize = new Vector2(200f, 20f);
    public Vector2 barPosition = new Vector2(20f, -20f);
    public Color fillColor = Color.red;
    public Color bgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private Image fillImage;
    private Canvas canvas;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        BuildUI();
    }

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("HpCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 배경 바
        var bgGO = new GameObject("HpBar_BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = bgColor;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 1f);
        bgRect.sizeDelta = barSize;
        bgRect.anchoredPosition = barPosition;

        // 채움 바
        var fillGO = new GameObject("HpBar_Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = fillColor;
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (playerHealth == null || fillImage == null)
            return;

        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.type = Image.Type.Filled;
        fillImage.fillAmount = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);
    }
}
