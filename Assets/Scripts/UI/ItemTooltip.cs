using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 / 상점에서 아이템 호버 시 이름·설명을 띄우는 툴팁.
///
/// 두 가지 모드 지원:
/// 1. 자동 생성: 씬에 ItemTooltip 컴포넌트가 없으면 첫 호출 시 캔버스를 찾아 패널을 코드로 생성.
/// 2. 수동 연결: 씬에 미리 ItemTooltip을 만들어 nameText/descText/iconImage를 인스펙터에서 연결한 경우 그걸 사용.
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("UI 연결 (수동 모드)")]
    public GameObject rootPanel;

    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private TextMeshProUGUI descText;

    [SerializeField]
    private Image iconImage;

    [Header("위치")]
    public Vector2 cursorOffset = new Vector2(16f, -16f);

    private RectTransform _rt;
    private Canvas _canvas;
    private RectTransform _canvasRT;
    private bool _isShown;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitRefs();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void InitRefs()
    {
        _rt = (rootPanel != null ? rootPanel.transform : transform) as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRT = _canvas.transform as RectTransform;
    }

    /// <summary>씬에 인스턴스가 없으면 캔버스를 찾아 코드로 툴팁 GameObject를 만들고 반환.</summary>
    public static ItemTooltip GetOrCreate(Canvas canvas = null)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            return null;

        var rootCanvas = canvas.rootCanvas;

        // 루트 캔버스 하위의 기존 TMP 텍스트에서 폰트/머티리얼을 빌려와 같은 룩앤필 유지
        TMP_FontAsset borrowedFont = null;
        Material borrowedMat = null;
        var sceneTmps = rootCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in sceneTmps)
        {
            if (t == null || t.font == null)
                continue;
            borrowedFont = t.font;
            borrowedMat = t.fontMaterial;
            break;
        }
        var panel = new GameObject(
            "ItemTooltip (Runtime)",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ItemTooltip)
        );
        panel.transform.SetParent(rootCanvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 110);
        rt.pivot = new Vector2(0, 1);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        bg.raycastTarget = false;

        var nameGO = CreateTMP(
            panel.transform,
            "NameText",
            out var nameTMP,
            18,
            FontStyles.Bold,
            borrowedFont,
            borrowedMat
        );
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -8);
        nameRT.sizeDelta = new Vector2(-16, 24);

        var descGO = CreateTMP(
            panel.transform,
            "DescText",
            out var descTMP,
            14,
            FontStyles.Normal,
            borrowedFont,
            borrowedMat
        );
        var descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0.5f, 1);
        descRT.anchoredPosition = new Vector2(0, -36);
        descRT.sizeDelta = new Vector2(-16, -44);
        descTMP.textWrappingMode = TextWrappingModes.Normal;

        var tt = panel.GetComponent<ItemTooltip>();
        tt.rootPanel = panel;
        tt.nameText = nameTMP;
        tt.descText = descTMP;
        tt.InitRefs();
        tt.Hide();

        return tt;
    }

    static GameObject CreateTMP(
        Transform parent,
        string name,
        out TextMeshProUGUI tmp,
        float fontSize,
        FontStyles style,
        TMP_FontAsset font = null,
        Material fontMaterial = null
    )
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        if (fontMaterial != null)
            tmp.fontMaterial = fontMaterial;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        return go;
    }

    void LateUpdate()
    {
        if (!_isShown || _canvasRT == null || _rt == null)
            return;

        Camera cam =
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT,
                Input.mousePosition,
                cam,
                out Vector2 local
            )
        )
            return;

        _rt.anchoredPosition = local + cursorOffset;
        ClampInsideCanvas();
    }

    public void Show(ScriptableObject item)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        string name = "";
        string desc = "";
        Sprite icon = null;

        if (item is WeaponData w)
        {
            name =
                w.weaponName != null && !w.weaponName.IsEmpty
                    ? w.weaponName.GetLocalizedString()
                    : w.name;
            desc =
                w.description != null && !w.description.IsEmpty
                    ? w.description.GetLocalizedString()
                    : "";
            icon = w.sprite;
        }
        else if (item is AccessoryData a)
        {
            name =
                a.accessoryName != null && !a.accessoryName.IsEmpty
                    ? a.accessoryName.GetLocalizedString()
                    : a.name;
            desc =
                a.description != null && !a.description.IsEmpty
                    ? a.description.GetLocalizedString()
                    : "";
            icon = a.icon;
        }
        else
        {
            name = item.name;
        }

        if (nameText != null)
            nameText.text = name;
        if (descText != null)
            descText.text = desc;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(true);
        _isShown = true;
    }

    public void Hide()
    {
        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(false);
        _isShown = false;
    }

    void ClampInsideCanvas()
    {
        if (_canvasRT == null || _rt == null)
            return;

        Vector2 canvasSize = _canvasRT.rect.size;
        Vector2 panelSize = _rt.rect.size;
        Vector2 pos = _rt.anchoredPosition;

        float minX = -canvasSize.x * 0.5f;
        float maxX = canvasSize.x * 0.5f - panelSize.x;
        float minY = -canvasSize.y * 0.5f + panelSize.y;
        float maxY = canvasSize.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        _rt.anchoredPosition = pos;
    }
}
