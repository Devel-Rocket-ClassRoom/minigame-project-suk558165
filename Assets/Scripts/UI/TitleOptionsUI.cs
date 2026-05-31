using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleOptionsUI : MonoBehaviour
{
    [Header("볼륨 슬라이더")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("언어")]
    public TMP_Dropdown languageDropdown;

    [Header("키 바인딩 텍스트")]
    public TextMeshProUGUI dashKeyText;
    public TextMeshProUGUI attackKeyText;
    public TextMeshProUGUI inventoryKeyText;
    public TextMeshProUGUI interactKeyText;

    private string rebindingAction;
    private bool isRebinding;

    private static readonly KeyCode[] blockedKeys =
    {
        KeyCode.Escape,
        KeyCode.Mouse0,
        KeyCode.Mouse1,
        KeyCode.Mouse2,
    };

    void Start()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        InitLanguageDropdown();
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
    }

    void Update()
    {
        if (!isRebinding)
            return;

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

    void InitLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "English", "한국어" });
        languageDropdown.SetValueWithoutNotify(
            LanguageManager.Instance != null ? LanguageManager.Instance.CurrentIndex : 0);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    void OnLanguageChanged(int index)
    {
        LanguageManager.Instance?.SetLanguage(index);
    }

    // ── 키 바인딩 ─────────────────────────────────────────

    void RefreshKeyLabels()
    {
        var im = InputManager.Instance;
        if (im == null)
            return;
        if (dashKeyText != null)
            dashKeyText.text = im.Dash.ToString();
        if (attackKeyText != null)
            attackKeyText.text = im.Attack.ToString();
        if (inventoryKeyText != null)
            inventoryKeyText.text = im.Inventory.ToString();
        if (interactKeyText != null)
            interactKeyText.text = im.Interact.ToString();
    }

    void StartRebind(string action, TextMeshProUGUI label)
    {
        isRebinding = true;
        rebindingAction = action;
        if (label != null)
            label.text = "키를 입력하세요...";
    }

    void CancelRebind()
    {
        isRebinding = false;
        rebindingAction = null;
        RefreshKeyLabels();
    }

    public void OnDashRebind() => StartRebind("Dash", dashKeyText);

    public void OnAttackRebind() => StartRebind("Attack", attackKeyText);

    public void OnInventoryRebind() => StartRebind("Inventory", inventoryKeyText);

    public void OnInteractRebind() => StartRebind("Interact", interactKeyText);

    // ── 닫기 ──────────────────────────────────────────────

    public void OnClose()
    {
        CancelRebind();
        gameObject.SetActive(false);
    }
}
