using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("Sprites (Hp.png = frame, gauge.png = fill)")]
    public Sprite frameSprite; // Hp.png  — skull border frame
    public Sprite gaugeSprite; // gauge.png — red fill bar
    public Font pixelFont; // DungGeunMo.ttf

    [Header("Bar Layout")]
    public Vector2 barSize = new Vector2(480f, 60f);
    public Vector2 barPosition = new Vector2(20f, -20f);

    [Header("Fill Area Padding (px at barSize scale)")]
    public float padLeft = 23f;
    public float padBottom = 5f;
    public float padRight = 23f;
    public float padTop = 5f;

    private RectTransform fillRect;
    private Text hpText;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void Start() => BuildUI();

    // ── Build UI ──────────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("HpCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Frame (Hp.png, 9-sliced) ──────────────────────────────────────────
        var frameGO = new GameObject("HpBar_Frame");
        frameGO.transform.SetParent(canvasGO.transform, false);
        var frameImg = frameGO.AddComponent<Image>();
        if (frameSprite != null)
        {
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
        }
        else
        {
            frameImg.color = new Color(0.08f, 0.07f, 0.12f, 1f);
        }
        var frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.anchorMin = frameRT.anchorMax = new Vector2(0f, 1f);
        frameRT.pivot = new Vector2(0f, 1f);
        frameRT.sizeDelta = barSize;
        frameRT.anchoredPosition = barPosition;

        // ── Fill mask ─────────────────────────────────────────────────────────
        var maskGO = new GameObject("HpBar_FillMask");
        maskGO.transform.SetParent(frameGO.transform, false);
        maskGO.AddComponent<RectMask2D>(); // RequireComponent adds RectTransform
        var maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.offsetMin = new Vector2(padLeft, padBottom);
        maskRT.offsetMax = new Vector2(-padRight, -padTop);

        // ── Gauge fill (gauge.png) ────────────────────────────────────────────
        var fillGO = new GameObject("HpBar_Gauge");
        fillGO.transform.SetParent(maskGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        if (gaugeSprite != null)
        {
            fillImg.sprite = gaugeSprite;
            fillImg.type = Image.Type.Simple;
            fillImg.preserveAspect = false;
        }
        else
        {
            fillImg.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        }
        fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // ── HP text ───────────────────────────────────────────────────────────
        var textGO = new GameObject("HpBar_Text");
        textGO.transform.SetParent(frameGO.transform, false);
        hpText = textGO.AddComponent<Text>();
        hpText.font = pixelFont != null ? pixelFont : Font.CreateDynamicFontFromOSFont("Arial", 14);
        hpText.fontSize = 20;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.horizontalOverflow = HorizontalWrapMode.Overflow;
        hpText.verticalOverflow = VerticalWrapMode.Overflow;
        var outl = textGO.AddComponent<Outline>();
        outl.effectColor = new Color(0f, 0f, 0f, 1f);
        outl.effectDistance = new Vector2(1, -1);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null || fillRect == null)
            return;

        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);

        // Shrink the fill bar from the right
        fillRect.anchorMax = new Vector2(ratio, 1f);

        // HP number display
        if (hpText != null)
        {
            int cur = Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp));
            int max = Mathf.CeilToInt(playerHealth.maxHp);
            hpText.text = $"{cur} / {max}";
        }
    }
}
