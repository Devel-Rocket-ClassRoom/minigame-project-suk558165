using Cysharp.Threading.Tasks;
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

    [Header("컨트롤")]
    [SerializeField] private Button controlButton;
    [SerializeField] private ControlPanelUI controlPanel;

    void OnEnable()
    {
        if (controlButton != null)
            controlButton.onClick.AddListener(OnControlButton);
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
        RefreshLocalizedAfterInit().Forget();

        RefreshVolume();
    }

    void OnDisable()
    {
        if (controlButton != null)
            controlButton.onClick.RemoveListener(OnControlButton);
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
        if (fullscreenDropdown != null)
        {
            int prev = fullscreenDropdown.value;
            SetupFullscreenDropdown();
            fullscreenDropdown.SetValueWithoutNotify(prev);
            fullscreenDropdown.RefreshShownValue();
        }
    }

    /// <summary>
    /// Localization 초기화 완료 후 로케일 문자열이 필요한 UI를 다시 채운다.
    /// OnEnable 시점에는 초기화가 끝나지 않아 폴백 문자열로 먼저 표시된다.
    /// </summary>
    async UniTaskVoid RefreshLocalizedAfterInit()
    {
        await LocalizationSettings.InitializationOperation;
        if (fullscreenDropdown == null)
            return;
        SetupFullscreenDropdown();
    }


    void Update()
    {
        if (controlPanel != null && controlPanel.gameObject.activeSelf)
            return;
        if (Input.GetKeyDown(KeyCode.Escape))
            OnClose();
    }

    // ── 공개 ──────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
    }

    // ── 볼륨 ──────────────────────────────────────────────

    const string PrefKeyMaster = "Vol_Master";
    const string PrefKeyBGM = "Vol_BGM";
    const string PrefKeySFX = "Vol_SFX";

    void RefreshVolume()
    {
        float master, bgm, sfx;

        if (SaveManager.Instance != null)
        {
            var d = SaveManager.Instance.Data;
            master = d.volumeMaster;
            bgm = d.volumeBGM;
            sfx = d.volumeSFX;
        }
        else
        {
            master = PlayerPrefs.GetFloat(PrefKeyMaster, 1f);
            bgm = PlayerPrefs.GetFloat(PrefKeyBGM, 1f);
            sfx = PlayerPrefs.GetFloat(PrefKeySFX, 1f);
        }

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(master);
        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgm);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfx);

        AudioListener.volume = master;
        AudioManager.Instance?.SetBGMVolume(bgm);
        AudioManager.Instance?.SetSFXVolume(sfx);
    }

    public void OnMasterChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(PrefKeyMaster, v);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.volumeMaster = v;
            SaveManager.Instance.Save();
        }
    }

    public void OnBGMChanged(float v)
    {
        AudioManager.Instance?.SetBGMVolume(v);
        PlayerPrefs.SetFloat(PrefKeyBGM, v);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.volumeBGM = v;
            SaveManager.Instance.Save();
        }
    }

    public void OnSFXChanged(float v)
    {
        AudioManager.Instance?.SetSFXVolume(v);
        PlayerPrefs.SetFloat(PrefKeySFX, v);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.volumeSFX = v;
            SaveManager.Instance.Save();
        }
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
            new TMP_Dropdown.OptionData(L10n.Get("ui.options.fullscreen_windowed", "창 모드")),
            new TMP_Dropdown.OptionData(
                L10n.Get("ui.options.fullscreen_borderless", "테두리 없는 창")
            ),
            new TMP_Dropdown.OptionData(
                L10n.Get("ui.options.fullscreen_exclusive", "전체화면")
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

    // ── 컨트롤 패널 ─────────────────────────────────────────

    public void OnControlButton()
    {
        if (controlPanel == null)
            return;

        if (!controlPanel.gameObject.scene.IsValid())
        {
            var canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform.parent;
            controlPanel = Instantiate(controlPanel, parent);
            controlPanel.gameObject.name = "ControlPanel";
        }

        controlPanel.Open();
    }

    // ── 닫기 ──────────────────────────────────────────────

    public void OnClose()
    {
        gameObject.SetActive(false);
        if (PauseMenu.IsPaused)
            PauseMenu.Instance?.OnOptionsBackButton();
    }
}
