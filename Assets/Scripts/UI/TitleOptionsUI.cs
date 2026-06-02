using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleOptionsUI : MonoBehaviour
{
    [Header("볼륨 슬라이더")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

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
        // 액션 설명 레이블을 정적 텍스트로 복원
        if (dashKeyText != null)
            dashKeyText.text = "대시";
        if (attackKeyText != null)
            attackKeyText.text = "공격";
        if (inventoryKeyText != null)
            inventoryKeyText.text = "인벤토리";
        if (interactKeyText != null)
            interactKeyText.text = "상호작용";

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
