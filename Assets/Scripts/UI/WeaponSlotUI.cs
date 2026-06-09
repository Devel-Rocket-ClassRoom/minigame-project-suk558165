using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("크기")]
    [SerializeField] private float frontSize = 70f;
    [SerializeField] private float backSize = 50f;
    [SerializeField] private float padding = 20f;

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

    private Vector2 frontTargetPos;
    private Vector2 backTargetPos;
    private Vector2 frontStartPos;
    private Vector2 backStartPos;
    private float swapTimer;
    private bool isSwapping;

    private Vector2 frontScale;
    private Vector2 backScale;
    private Vector2 frontStartScale;
    private Vector2 backStartScale;

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

        float totalW = frontSize + backSize * 0.4f;
        float totalH = frontSize + backSize * 0.3f;
        containerRect.sizeDelta = new Vector2(totalW, totalH);

        BuildSlot(container.transform, "BackSlot", backSize, out backRect, out backBg, out backIcon);
        backRect.anchoredPosition = new Vector2(frontSize * 0.5f, frontSize * 0.45f);

        BuildSlot(container.transform, "FrontSlot", frontSize, out frontRect, out frontBg, out frontIcon);
        frontRect.anchoredPosition = new Vector2(0f, 0f);

        frontTargetPos = frontRect.anchoredPosition;
        backTargetPos = backRect.anchoredPosition;
        frontScale = Vector2.one;
        backScale = new Vector2(backSize / frontSize, backSize / frontSize);

        backBg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        frontBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    }

    void BuildSlot(Transform parent, string name, float size,
        out RectTransform rect, out Image bg, out Image icon)
    {
        var slotGo = new GameObject(name);
        slotGo.transform.SetParent(parent, false);
        rect = slotGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
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

        if (isSwapping)
        {
            swapTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(swapTimer / swapDuration);
            t = t * t * (3f - 2f * t);

            frontRect.anchoredPosition = Vector2.Lerp(frontStartPos, frontTargetPos, t);
            backRect.anchoredPosition = Vector2.Lerp(backStartPos, backTargetPos, t);

            frontRect.localScale = Vector3.Lerp(frontStartScale, frontScale, t);
            backRect.localScale = Vector3.Lerp(backStartScale, backScale, t);

            if (t >= 1f)
                isSwapping = false;
        }
    }

    void OnWeaponChanged(WeaponData _)
    {
        PlaySwap();
        Refresh();
    }

    void PlaySwap()
    {
        frontStartPos = backTargetPos;
        backStartPos = frontTargetPos;
        frontStartScale = backScale;
        backStartScale = frontScale;
        swapTimer = 0f;
        isSwapping = true;
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
