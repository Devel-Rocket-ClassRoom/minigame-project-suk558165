using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class TitleOptionsUI : MonoBehaviour
{
    [Header("볼륨 슬라이더")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("언어")]
    [Tooltip("언어 선택 드롭다운 (옵션). 항목 순서: English, 한국어")]
    public TMP_Dropdown languageDropdown;

    [Header("디스플레이")]
    [Tooltip("해상도 선택 드롭다운 (옵션)")]
    public TMP_Dropdown resolutionDropdown;

    [Tooltip("화면 모드 드롭다운 (옵션). 항목 순서: 창 모드, 테두리 없는 창, 전체화면")]
    public TMP_Dropdown fullscreenDropdown;

    [Header("키 바인딩 레이블 (액션 설명 텍스트에 연결)")]
    public TextMeshProUGUI dashKeyText;
    public TextMeshProUGUI attackKeyText;
    public TextMeshProUGUI inventoryKeyText;
    public TextMeshProUGUI interactKeyText;

    [Header("중복 경고")]
    [SerializeField]
    private TextMeshProUGUI duplicateWarningText;

    private TextMeshProUGUI dashKeyDisplay;
    private TextMeshProUGUI attackKeyDisplay;
    private TextMeshProUGUI inventoryKeyDisplay;
    private TextMeshProUGUI interactKeyDisplay;

    private string rebindingAction;
    private bool isRebinding;

    private static readonly KeyCode[] blockedKeys =
    {
        KeyCode.Escape,
        KeyCode.Mouse0,
        KeyCode.Mouse1,
        KeyCode.Mouse2,
    };

    void Awake()
    {
        // 액션 설명 레이블 — 로케일에 맞춘 텍스트로 설정 (테이블 미초기화면 한국어 기본)
        if (dashKeyText != null)
            dashKeyText.text = GetLocalized("ui.options.dash", "대시");
        if (attackKeyText != null)
            attackKeyText.text = GetLocalized("ui.options.attack", "공격");
        if (inventoryKeyText != null)
            inventoryKeyText.text = GetLocalized("ui.options.inventory", "인벤토리");
        if (interactKeyText != null)
            interactKeyText.text = GetLocalized("ui.options.interact", "상호작용");

        // 어두운 KeyText 박스 안에 키 값 표시용 TMP 동적 생성
        dashKeyDisplay = CreateKeyDisplay(dashKeyText);
        attackKeyDisplay = CreateKeyDisplay(attackKeyText);
        inventoryKeyDisplay = CreateKeyDisplay(inventoryKeyText);
        interactKeyDisplay = CreateKeyDisplay(interactKeyText);
    }

    void OnEnable()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        if (languageDropdown != null)
        {
            SetupLanguageDropdown();
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        if (resolutionDropdown != null)
        {
            SetupResolutionDropdown();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (fullscreenDropdown != null)
        {
            SetupFullscreenDropdown();
            fullscreenDropdown.onValueChanged.AddListener(OnFullscreenChanged);
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        RefreshVolume();
        RefreshKeyLabels();
    }

    void OnDisable()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (fullscreenDropdown != null)
            fullscreenDropdown.onValueChanged.RemoveListener(OnFullscreenChanged);

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale _)
    {
        // 언어 변경 시 드롭다운의 동적 라벨(전체화면 등) 다시 채우기
        if (fullscreenDropdown != null)
        {
            int prev = fullscreenDropdown.value;
            SetupFullscreenDropdown();
            fullscreenDropdown.SetValueWithoutNotify(prev);
            fullscreenDropdown.RefreshShownValue();
        }

        // 액션 라벨 — Awake에서만 설정되므로 여기서 다시 한 번 갱신
        if (dashKeyText != null)
            dashKeyText.text = GetLocalized("ui.options.dash", "대시");
        if (attackKeyText != null)
            attackKeyText.text = GetLocalized("ui.options.attack", "공격");
        if (inventoryKeyText != null)
            inventoryKeyText.text = GetLocalized("ui.options.inventory", "인벤토리");
        if (interactKeyText != null)
            interactKeyText.text = GetLocalized("ui.options.interact", "상호작용");
    }

    static string GetLocalized(string key, string fallback)
    {
        var table = LocalizationSettings.StringDatabase?.GetTable("Items");
        if (table == null)
            return fallback;
        var entry = table.GetEntry(key);
        return entry != null ? entry.GetLocalizedString() : fallback;
    }

    void Update()
    {
        if (!isRebinding)
        {
            // 리바인딩 중이 아닐 때 ESC → 옵션 패널 닫기
            if (Input.GetKeyDown(KeyCode.Escape))
                OnClose();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (System.Array.IndexOf(blockedKeys, kc) >= 0)
                continue;
            if (!Input.GetKeyDown(kc))
                continue;

            if (IsDuplicateKey(kc))
            {
                ShowWarning("중복된 키입니다");
                return;
            }

            HideWarning();
            InputManager.Instance?.SetKey(rebindingAction, kc);
            isRebinding = false;
            rebindingAction = null;
            RefreshKeyLabels();
            return;
        }
    }

    // ── 공개 ──────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
    }

    // ── 볼륨 ──────────────────────────────────────────────

    void RefreshVolume()
    {
        if (SaveManager.Instance == null)
            return;
        var d = SaveManager.Instance.Data;
        if (masterSlider != null)
            masterSlider.value = d.volumeMaster;
        if (bgmSlider != null)
            bgmSlider.value = d.volumeBGM;
        if (sfxSlider != null)
            sfxSlider.value = d.volumeSFX;
    }

    public void OnMasterChanged(float v)
    {
        AudioListener.volume = v;
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeMaster = v;
        SaveManager.Instance?.Save();
    }

    public void OnBGMChanged(float v)
    {
        AudioManager.Instance?.SetBGMVolume(v);
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeBGM = v;
        SaveManager.Instance?.Save();
    }

    public void OnSFXChanged(float v)
    {
        AudioManager.Instance?.SetSFXVolume(v);
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeSFX = v;
        SaveManager.Instance?.Save();
    }

    // ── 언어 ──────────────────────────────────────────────

    void SetupLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        if (languageDropdown.template == null)
        {
            Debug.LogWarning(
                "[TitleOptionsUI] languageDropdown.template이 비어 있습니다. Editor에서 TMP Dropdown의 Template 필드를 연결하세요."
            );
            return;
        }

        // 인스펙터에 더미 항목(Option A/B/C)이 있어도 항상 덮어쓴다
        languageDropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("English"),
            new TMP_Dropdown.OptionData("한국어"),
        };

        // onValueChanged 발화 없이 현재 값만 설정
        int current = LanguageManager.Instance != null ? LanguageManager.Instance.CurrentIndex : 0;
        languageDropdown.SetValueWithoutNotify(current);
        languageDropdown.RefreshShownValue();
    }

    public void OnLanguageChanged(int index)
    {
        LanguageManager.Instance?.SetLanguage(index);
    }

    // ── 디스플레이 ────────────────────────────────────────

    void SetupResolutionDropdown()
    {
        var dm = DisplayManager.Instance;
        if (dm == null || resolutionDropdown == null)
            return;

        if (resolutionDropdown.template == null)
        {
            Debug.LogWarning(
                "[TitleOptionsUI] resolutionDropdown.template이 비어 있습니다. Editor에서 TMP Dropdown의 Template 필드를 연결하세요."
            );
            return;
        }

        var opts = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var r in dm.AvailableResolutions)
            opts.Add(new TMP_Dropdown.OptionData($"{r.x} x {r.y}"));
        resolutionDropdown.options = opts;
        resolutionDropdown.SetValueWithoutNotify(dm.GetCurrentResolutionIndex());
        resolutionDropdown.RefreshShownValue();
    }

    void SetupFullscreenDropdown()
    {
        if (fullscreenDropdown == null)
            return;

        if (fullscreenDropdown.template == null)
        {
            Debug.LogWarning(
                "[TitleOptionsUI] fullscreenDropdown.template이 비어 있습니다. Editor에서 TMP Dropdown의 Template 필드를 연결하세요."
            );
            return;
        }

        // 인스펙터 더미 항목 무시하고 항상 덮어쓴다. 로케일별로 라벨 자동 적용
        fullscreenDropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData(GetLocalized("ui.options.fullscreen_windowed", "창 모드")),
            new TMP_Dropdown.OptionData(
                GetLocalized("ui.options.fullscreen_borderless", "테두리 없는 창")
            ),
            new TMP_Dropdown.OptionData(
                GetLocalized("ui.options.fullscreen_exclusive", "전체화면")
            ),
        };

        int idx = FullscreenModeToIndex(Screen.fullScreenMode);
        fullscreenDropdown.SetValueWithoutNotify(idx);
        fullscreenDropdown.RefreshShownValue();
    }

    public void OnResolutionChanged(int index)
    {
        var dm = DisplayManager.Instance;
        if (dm == null || index < 0 || index >= dm.AvailableResolutions.Count)
            return;
        var r = dm.AvailableResolutions[index];
        dm.SetResolution(r.x, r.y);
    }

    public void OnFullscreenChanged(int index)
    {
        DisplayManager.Instance?.SetFullscreenMode(IndexToFullscreenMode(index));
    }

    static int FullscreenModeToIndex(FullScreenMode mode) =>
        mode switch
        {
            FullScreenMode.Windowed => 0,
            FullScreenMode.FullScreenWindow => 1,
            FullScreenMode.ExclusiveFullScreen => 2,
            FullScreenMode.MaximizedWindow => 1,
            _ => 1,
        };

    static FullScreenMode IndexToFullscreenMode(int index) =>
        index switch
        {
            0 => FullScreenMode.Windowed,
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.FullScreenWindow,
        };

    // ── 키 바인딩 ─────────────────────────────────────────

    void RefreshKeyLabels()
    {
        var im = InputManager.Instance;
        if (im == null)
            return;
        if (dashKeyDisplay != null)
            dashKeyDisplay.text = im.Dash.ToString();
        if (attackKeyDisplay != null)
            attackKeyDisplay.text = im.Attack.ToString();
        if (inventoryKeyDisplay != null)
            inventoryKeyDisplay.text = im.Inventory.ToString();
        if (interactKeyDisplay != null)
            interactKeyDisplay.text = im.Interact.ToString();
    }

    TextMeshProUGUI CreateKeyDisplay(TextMeshProUGUI actionLabel)
    {
        if (actionLabel == null)
            return null;

        var row = actionLabel.transform.parent;
        if (row == null)
            return null;

        var keyBox = row.Find("KeyText");
        if (keyBox == null)
            return null;

        var go = new GameObject("KeyValue");
        go.transform.SetParent(keyBox, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = actionLabel.font;
        tmp.fontSize = actionLabel.fontSize;
        tmp.fontMaterial = actionLabel.fontMaterial;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;
    }

    bool IsDuplicateKey(KeyCode kc)
    {
        var im = InputManager.Instance;
        if (im == null)
            return false;

        return (rebindingAction != "Dash" && im.Dash == kc)
            || (rebindingAction != "Attack" && im.Attack == kc)
            || (rebindingAction != "Inventory" && im.Inventory == kc)
            || (rebindingAction != "Interact" && im.Interact == kc);
    }

    void ShowWarning(string message)
    {
        if (duplicateWarningText == null)
            return;
        duplicateWarningText.text = message;
        duplicateWarningText.gameObject.SetActive(true);
    }

    void HideWarning()
    {
        if (duplicateWarningText != null)
            duplicateWarningText.gameObject.SetActive(false);
    }

    void StartRebind(string action, TextMeshProUGUI display)
    {
        HideWarning();
        isRebinding = true;
        rebindingAction = action;
        if (display != null)
            display.text = "...";
    }

    void CancelRebind()
    {
        HideWarning();
        isRebinding = false;
        rebindingAction = null;
        RefreshKeyLabels();
    }

    public void OnDashRebind() => StartRebind("Dash", dashKeyDisplay);

    public void OnAttackRebind() => StartRebind("Attack", attackKeyDisplay);

    public void OnInventoryRebind() => StartRebind("Inventory", inventoryKeyDisplay);

    public void OnInteractRebind() => StartRebind("Interact", interactKeyDisplay);

    // ── 닫기 ──────────────────────────────────────────────

    public void OnClose()
    {
        CancelRebind();
        gameObject.SetActive(false);
        if (PauseMenu.IsPaused)
            PauseMenu.Instance?.OnOptionsBackButton();
    }
}
