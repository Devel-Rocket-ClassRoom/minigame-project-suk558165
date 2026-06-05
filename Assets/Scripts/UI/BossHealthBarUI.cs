using UnityEngine;
using UnityEngine.UI;

// 화면 하단에 고정되는 큰 보스 HP바. 런타임에 Canvas+Image를 생성한다.
public class BossHealthBarUI : MonoBehaviour
{
    public float drainSpeed = 1.5f;
    public string bossName = "BOSS";

    [Header("크기/위치")]
    public Vector2 size = new Vector2(1200f, 40f);
    public float bottomOffset = 60f;

    private Image fill;
    private RectTransform fillRect;
    private float targetRatio = 1f;
    private float displayRatio = 1f;
    private GameObject rootGo;

    public static BossHealthBarUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        Build();
        SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (rootGo != null)
            Destroy(rootGo);
    }

    void Build()
    {
        rootGo = new GameObject("BossHealthBarUI_Canvas");
        var canvas = rootGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = rootGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        rootGo.AddComponent<GraphicRaycaster>();

        // 컨테이너
        var container = new GameObject("Container");
        container.transform.SetParent(rootGo.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, bottomOffset);
        containerRect.sizeDelta = size;

        // 배경
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(container.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        // 보더
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(container.transform, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-4f, -4f);
        borderRect.offsetMax = new Vector2(4f, 4f);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = new Color(0.8f, 0.7f, 0.2f, 1f);
        borderGo.transform.SetSiblingIndex(0);

        // 필 이미지 (HP)
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(container.transform, false);
        fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.85f, 0.15f, 0.15f, 1f);

        // 보스 이름 텍스트
        var nameGo = new GameObject("BossName");
        nameGo.transform.SetParent(container.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 6f);
        nameRect.sizeDelta = new Vector2(0f, 36f);
        var nameText = nameGo.AddComponent<Text>();
        nameText.text = bossName;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.fontSize = 28;
        nameText.color = new Color(1f, 0.95f, 0.8f, 1f);
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Update()
    {
        if (fillRect == null)
            return;
        if (!Mathf.Approximately(displayRatio, targetRatio))
        {
            displayRatio = Mathf.MoveTowards(
                displayRatio,
                targetRatio,
                Time.deltaTime * drainSpeed
            );
            ApplyFillScale();
        }
    }

    void ApplyFillScale()
    {
        if (fillRect == null)
            return;
        Vector3 s = fillRect.localScale;
        fillRect.localScale = new Vector3(displayRatio, s.y, s.z);
    }

    public void SetActive(bool active)
    {
        if (rootGo != null)
            rootGo.SetActive(active);
    }

    public void Show(string name)
    {
        bossName = name;
        var nameText = rootGo != null ? rootGo.GetComponentInChildren<Text>() : null;
        if (nameText != null)
            nameText.text = name;
        targetRatio = 1f;
        displayRatio = 1f;
        ApplyFillScale();
        SetActive(true);
    }

    public void SetHealth(float current, float max)
    {
        targetRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    public void Hide() => SetActive(false);
}
