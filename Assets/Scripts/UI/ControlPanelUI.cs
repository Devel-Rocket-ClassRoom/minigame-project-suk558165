using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelUI : MonoBehaviour
{
    [System.Serializable]
    public class KeyRow
    {
        public string action;
        public string label;
        public string locKey;
        public TextMeshProUGUI actionText;
        public TextMeshProUGUI keyDisplay;
        public Button button;
    }

    [SerializeField] private KeyRow[] rows;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Button closeButton;

    private string rebindingAction;
    private bool isRebinding;
    private GameObject overlay;

    private static readonly KeyCode[] blockedKeys =
    {
        KeyCode.Escape,
        KeyCode.Mouse0,
        KeyCode.Mouse1,
        KeyCode.Mouse2,
    };

    void Awake()
    {
        closeButton?.onClick.AddListener(Close);
        foreach (var row in rows)
        {
            if (row.button == null) continue;
            var capturedAction = row.action;
            var capturedDisplay = row.keyDisplay;
            row.button.onClick.AddListener(() => StartRebind(capturedAction, capturedDisplay));
        }
    }

    void OnEnable()
    {
        RefreshAll();
    }

    public void Open()
    {
        EnsureOverlay();
        overlay.SetActive(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        CancelRebind();
        gameObject.SetActive(false);
        if (overlay != null)
            overlay.SetActive(false);
    }

    void EnsureOverlay()
    {
        if (overlay != null)
            return;

        overlay = new GameObject("ControlPanelOverlay", typeof(RectTransform));
        overlay.transform.SetParent(transform.parent, false);

        var rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = overlay.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        img.raycastTarget = true;

        overlay.transform.SetSiblingIndex(transform.GetSiblingIndex());
    }

    void Update()
    {
        if (!isRebinding)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
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
                ShowWarning(L10n.Get("ui.control.duplicate", "중복된 키입니다"));
                return;
            }

            HideWarning();
            InputManager.Instance?.SetKey(rebindingAction, kc);
            isRebinding = false;
            rebindingAction = null;
            RefreshAll();
            return;
        }
    }

    void RefreshAll()
    {
        var im = InputManager.Instance;
        if (im == null || rows == null)
            return;

        foreach (var row in rows)
        {
            if (row.actionText != null)
                row.actionText.text = L10n.Get(row.locKey, row.label);
            if (row.keyDisplay != null)
                row.keyDisplay.text = GetKey(im, row.action).ToString();
        }
    }

    static KeyCode GetKey(InputManager im, string action) =>
        action switch
        {
            "MoveLeft" => im.MoveLeft,
            "MoveRight" => im.MoveRight,
            "MoveDown" => im.MoveDown,
            "Jump" => im.Jump,
            "Dash" => im.Dash,
            "Attack" => im.Attack,
            "Inventory" => im.Inventory,
            "Interact" => im.Interact,
            "WeaponSwitch" => im.WeaponSwitch,
            _ => KeyCode.None,
        };

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
        RefreshAll();
    }

    bool IsDuplicateKey(KeyCode kc)
    {
        var im = InputManager.Instance;
        if (im == null || rows == null)
            return false;

        foreach (var row in rows)
        {
            if (row.action == rebindingAction)
                continue;
            if (GetKey(im, row.action) == kc)
                return true;
        }
        return false;
    }

    void ShowWarning(string msg)
    {
        if (warningText == null)
            return;
        warningText.text = msg;
        warningText.gameObject.SetActive(true);
    }

    void HideWarning()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }
}
