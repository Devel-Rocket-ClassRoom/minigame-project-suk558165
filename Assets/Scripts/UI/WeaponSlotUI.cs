using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("크기")]
    [SerializeField] private float frontSize = 100f;
    [SerializeField] private float backSize = 70f;
    [SerializeField] private float padding = 10f;

    [Header("애니메이션")]
    [SerializeField] private float swapDuration = 0.15f;

    public static WeaponSlotUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    private GameObject rootGo;
    private RectTransform frontRect;
    private RectTransform backRect;
    private Image frontIcon;
    private Image backIcon;
    private Image frontBg;
    private Image backBg;

    private WeaponInventory weaponInventory;

    private Vector2 frontPosFrom, frontPosTo;
    private Vector2 backPosFrom, backPosTo;
    private float frontSizeFrom, frontSizeTo;
    private float backSizeFrom, backSizeTo;
    private float swapTimer;
    private bool isSwapping;

    private Vector2 frontRestPos;
    private Vector2 backRestPos;

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
        if (weaponInventory != null)
            weaponInventory.OnWeaponChanged -= OnWeaponChanged;
        if (rootGo != null)
            Destroy(rootGo);
    }

    void Build()
    {
        rootGo = new GameObject("WeaponSlotUI_Canvas");
        var canvas = rootGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = rootGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        rootGo.AddComponent<GraphicRaycaster>();

        var container = new GameObject("Container");
        container.transform.SetParent(rootGo.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(0f, 0f);
        containerRect.pivot = new Vector2(0f, 0f);
        containerRect.anchoredPosition = new Vector2(padding, padding);
        containerRect.sizeDelta = new Vector2(frontSize + backSize, frontSize + backSize * 0.5f);

        BuildSlot(container.transform, "BackSlot", out backRect, out backBg, out backIcon);
        BuildSlot(container.transform, "FrontSlot", out frontRect, out frontBg, out frontIcon);

        frontRestPos = new Vector2(0f, 0f);
        backRestPos = new Vector2(frontSize * 0.55f, frontSize * 0.4f);

        frontRect.sizeDelta = new Vector2(frontSize, frontSize);
        frontRect.anchoredPosition = frontRestPos;

        backRect.sizeDelta = new Vector2(backSize, backSize);
        backRect.anchoredPosition = backRestPos;

        backBg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        frontBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    }

    void BuildSlot(Transform parent, string name,
        out RectTransform rect, out Image bg, out Image icon)
    {
        var slotGo = new GameObject(name);
        slotGo.transform.SetParent(parent, false);
        rect = slotGo.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0f, 0f);

        bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(slotGo.transform, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2f, -2f);
        borderRect.offsetMax = new Vector2(2f, 2f);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        borderGo.transform.SetSiblingIndex(0);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;
    }

    void Update()
    {
        if (weaponInventory == null || weaponInventory.gameObject == null)
        {
            weaponInventory = null;
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                return;
            weaponInventory = player.GetComponentInChildren<WeaponInventory>();
            if (weaponInventory == null)
                return;
            weaponInventory.OnWeaponChanged += OnWeaponChanged;
            Refresh();
        }

        if (!isSwapping)
            return;

        swapTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(swapTimer / swapDuration);
        t = t * t * (3f - 2f * t);

        frontRect.anchoredPosition = Vector2.Lerp(frontPosFrom, frontPosTo, t);
        backRect.anchoredPosition = Vector2.Lerp(backPosFrom, backPosTo, t);

        float fs = Mathf.Lerp(frontSizeFrom, frontSizeTo, t);
        frontRect.sizeDelta = new Vector2(fs, fs);

        float bs = Mathf.Lerp(backSizeFrom, backSizeTo, t);
        backRect.sizeDelta = new Vector2(bs, bs);

        if (t >= 1f)
        {
            isSwapping = false;
            frontRect.SetAsLastSibling();
        }
    }

    void OnWeaponChanged(WeaponData _)
    {
        PlaySwap();
        Refresh();
    }

    void PlaySwap()
    {
        frontPosFrom = backRestPos;
        frontPosTo = frontRestPos;
        backPosFrom = frontRestPos;
        backPosTo = backRestPos;

        frontSizeFrom = backSize;
        frontSizeTo = frontSize;
        backSizeFrom = frontSize;
        backSizeTo = backSize;

        swapTimer = 0f;
        isSwapping = true;
        backRect.SetAsLastSibling();
    }

    void Refresh()
    {
        if (weaponInventory == null)
            return;

        var weapons = weaponInventory.weapons;
        int current = weaponInventory.currentIndex;

        if (weapons.Count == 0)
        {
            frontBg.gameObject.SetActive(false);
            backBg.gameObject.SetActive(false);
            return;
        }

        frontBg.gameObject.SetActive(true);

        if (weapons.Count == 1)
        {
            backBg.gameObject.SetActive(false);
            SetIcon(frontIcon, weapons[0]);
            return;
        }

        backBg.gameObject.SetActive(true);
        SetIcon(frontIcon, weapons[current]);
        int other = (current + 1) % weapons.Count;
        SetIcon(backIcon, weapons[other]);
    }

    void SetIcon(Image icon, WeaponData weapon)
    {
        if (weapon != null && weapon.sprite != null)
        {
            icon.sprite = weapon.sprite;
            icon.enabled = true;
        }
        else
        {
            icon.enabled = false;
        }
    }

    public void SetActive(bool active)
    {
        if (rootGo != null)
            rootGo.SetActive(active);
    }
}
