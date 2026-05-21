using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("Sprites")]
    public Sprite frameSprite;
    public Sprite gaugeSprite;
    public Font pixelFont;

    [Header("Bar Layout")]
    public Vector2 barSize = new Vector2(480f, 60f);
    public Vector2 barPosition = new Vector2(20f, -20f);

    [Header("Fill Area Padding (px)")]
    public float padLeft = 23f;
    public float padBottom = 5f;
    public float padRight = 23f;
    public float padTop = 5f;

    private RectTransform fillRect;
    private Text hpText;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? FindFirstObjectByType<PlayerHealth>();
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        var canvas = CreateCanvas();
        var frame = CreateFrame(canvas.transform);
        var mask = CreateMask(frame);
        fillRect = CreateFill(mask).GetComponent<RectTransform>();
        hpText = CreateHpText(frame);
    }

    Canvas CreateCanvas()
    {
        var go = new GameObject("HpCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    RectTransform CreateFrame(Transform parent)
    {
        var go = new GameObject("HpBar_Frame");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
        }
        else
        {
            img.color = new Color(0.08f, 0.07f, 0.12f, 1f);
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = barSize;
        rt.anchoredPosition = barPosition;
        return rt;
    }

    RectTransform CreateMask(RectTransform parent)
    {
        var go = new GameObject("HpBar_FillMask");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectMask2D>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padLeft, padBottom);
        rt.offsetMax = new Vector2(-padRight, -padTop);
        return rt;
    }

    GameObject CreateFill(RectTransform parent)
    {
        var go = new GameObject("HpBar_Gauge");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        if (gaugeSprite != null)
        {
            img.sprite = gaugeSprite;
            img.preserveAspect = false;
        }
        else
        {
            img.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    Text CreateHpText(RectTransform parent)
    {
        var go = new GameObject("HpBar_Text");
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = pixelFont != null ? pixelFont : Font.CreateDynamicFontFromOSFont("Arial", 14);
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return text;
    }

    void Update()
    {
        if (playerHealth == null || fillRect == null)
            return;

        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);
        fillRect.anchorMax = new Vector2(ratio, 1f);

        if (hpText != null)
            hpText.text =
                $"{Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp))} / {Mathf.CeilToInt(playerHealth.maxHp)}";
    }
}
