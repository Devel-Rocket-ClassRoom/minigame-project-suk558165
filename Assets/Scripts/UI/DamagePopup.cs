using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private static Canvas popupCanvas;
    private static Camera mainCam;
    private static TMP_FontAsset cachedFont;

    private TextMeshProUGUI label;
    private RectTransform rect;
    private float elapsed;
    private float duration;
    private Vector3 worldPos;
    private Vector2 velocity;
    private Color color;

    const float DefaultDuration = 0.8f;
    const float RiseSpeed = 80f;
    const float SpreadX = 40f;
    const float FontSize = 32f;
    const float CritFontSize = 42f;
    const float HealFontSize = 30f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        popupCanvas = null;
        mainCam = null;
        cachedFont = null;
    }

    public static void Spawn(Vector3 position, float damage, bool isCrit = false, bool isPlayerDamage = false)
    {
        EnsureCanvas();
        var go = new GameObject("DmgPopup");
        go.transform.SetParent(popupCanvas.transform, false);
        var popup = go.AddComponent<DamagePopup>();
        popup.Init(position, damage, isCrit, isPlayerDamage, false);
    }

    public static void SpawnHeal(Vector3 position, float amount)
    {
        EnsureCanvas();
        var go = new GameObject("HealPopup");
        go.transform.SetParent(popupCanvas.transform, false);
        var popup = go.AddComponent<DamagePopup>();
        popup.Init(position, amount, false, false, true);
    }

    static void EnsureCanvas()
    {
        if (popupCanvas != null)
            return;

        var canvasGo = new GameObject("DamagePopup_Canvas");
        DontDestroyOnLoad(canvasGo);
        popupCanvas = canvasGo.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    static TMP_FontAsset GetFont()
    {
        if (cachedFont == null)
            cachedFont = Resources.Load<TMP_FontAsset>("MaruMinyaHangul SDF");
        return cachedFont;
    }

    void Init(Vector3 position, float value, bool isCrit, bool isPlayerDamage, bool isHeal)
    {
        worldPos = position;
        duration = DefaultDuration;
        rect = GetComponent<RectTransform>();

        label = gameObject.AddComponent<TextMeshProUGUI>();
        label.font = GetFont();
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Overflow;
        label.enableWordWrapping = false;
        label.raycastTarget = false;

        int display = Mathf.RoundToInt(value);

        if (isHeal)
        {
            label.fontSize = HealFontSize;
            color = new Color(0.2f, 0.9f, 0.3f, 1f);
            label.text = "+" + display;
        }
        else if (isPlayerDamage)
        {
            label.fontSize = FontSize;
            color = new Color(1f, 0.3f, 0.3f, 1f);
            label.text = display.ToString();
        }
        else if (isCrit)
        {
            label.fontSize = CritFontSize;
            label.fontStyle = FontStyles.Bold;
            color = new Color(1f, 0.5f, 0f, 1f);
            label.text = display + "!";
        }
        else
        {
            label.fontSize = FontSize;
            color = new Color(1f, 0.85f, 0.1f, 1f);
            label.text = display.ToString();
        }

        label.color = color;
        label.outlineWidth = 0.3f;
        label.outlineColor = new Color32(0, 0, 0, 200);

        float xSpread = isHeal ? Random.Range(-15f, 15f) : Random.Range(-SpreadX, SpreadX);
        velocity = new Vector2(xSpread, RiseSpeed);

        UpdateScreenPos();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= RiseSpeed * Time.deltaTime;
        rect.anchoredPosition += velocity * Time.deltaTime;

        float t = elapsed / duration;
        float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
        label.color = new Color(color.r, color.g, color.b, alpha);

        float scale = 1f;
        if (t < 0.1f)
            scale = Mathf.Lerp(0.5f, 1.2f, t / 0.1f);
        else if (t < 0.2f)
            scale = Mathf.Lerp(1.2f, 1f, (t - 0.1f) / 0.1f);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    void UpdateScreenPos()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null)
            return;

        Vector3 screen = mainCam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupCanvas.GetComponent<RectTransform>(), screen, null, out var local);
        rect.anchoredPosition = local;
    }
}
