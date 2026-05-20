using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("Bar Settings")]
    public Vector2 barSize = new Vector2(200f, 28f);
    public Vector2 barPosition = new Vector2(20f, -20f);
    public float borderWidth = 3f;

    private Image fillImage;
    private Text hpText;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HpCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 외곽선 (검정 테두리)
        var borderGO = new GameObject("HpBar_Border");
        borderGO.transform.SetParent(canvasGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        var borderRect = borderGO.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.pivot = new Vector2(0f, 1f);
        borderRect.sizeDelta = barSize + new Vector2(borderWidth * 2, borderWidth * 2);
        borderRect.anchoredPosition = barPosition;

        // 배경 (어두운 빨강)
        var bgGO = new GameObject("HpBar_BG");
        bgGO.transform.SetParent(borderGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.05f, 0.05f, 1f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(borderWidth, borderWidth);
        bgRect.offsetMax = new Vector2(-borderWidth, -borderWidth);

        // 채움 바 (빨강)
        var fillGO = new GameObject("HpBar_Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // HP 텍스트
        var textGO = new GameObject("HpBar_Text");
        textGO.transform.SetParent(borderGO.transform, false);
        hpText = textGO.AddComponent<Text>();
        hpText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        hpText.fontSize = 16;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var outl = textGO.AddComponent<Outline>();
        outl.effectColor = new Color(0, 0, 0, 0.8f);
        outl.effectDistance = new Vector2(1, -1);

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (playerHealth == null || fillImage == null)
            return;

        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);
        fillImage.fillAmount = ratio;

        if (hpText != null)
        {
            int cur = Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp));
            int max = Mathf.CeilToInt(playerHealth.maxHp);
            hpText.text = cur + " / " + max;
        }
    }
}
