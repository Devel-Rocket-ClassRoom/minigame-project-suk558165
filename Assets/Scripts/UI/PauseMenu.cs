using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsPaused = false;
    }

    [Header("Panels")]
    public CanvasGroup pausePanel;

    [SerializeField]
    private GameObject quitConfirmPanel;

    [Header("옵션 패널 (TitleOptionsPanel 프리팹 연결)")]
    [SerializeField]
    private TitleOptionsUI optionsPanelPrefab;

    private TitleOptionsUI optionsPanelInstance;

    void Awake()
    {
        Instance = this;
        HidePanel(pausePanel);
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);

        if (optionsPanelPrefab != null)
        {
            optionsPanelInstance = Instantiate(optionsPanelPrefab, transform.root);
            optionsPanelInstance.gameObject.SetActive(false);
        }
    }

    // ── GameFlowController 에서 호출 ──────────────────────

    public void Open()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        ShowPanel(pausePanel);
        optionsPanelInstance?.gameObject.SetActive(false);
    }

    public void Close()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        HidePanel(pausePanel);
        optionsPanelInstance?.gameObject.SetActive(false);
    }

    // ── 버튼 콜백 ─────────────────────────────────────────

    public void OnResumeButton()
    {
        GameFlowController.Instance?.ClosePauseMenu();
    }

    public void OnOptionsButton()
    {
        HidePanel(pausePanel);
        optionsPanelInstance?.Show();
    }

    public void OnOptionsBackButton()
    {
        optionsPanelInstance?.gameObject.SetActive(false);
        ShowPanel(pausePanel);
    }

    public void OnQuitButton()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
            return;
        }
        DoQuit();
    }

    public void OnQuitConfirm()
    {
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
        DoQuit();
    }

    public void OnQuitCancel()
    {
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    void DoQuit()
    {
        GameFlowController.Instance?.ClosePauseMenu();
        GameFlowController.Instance?.GoToTitle();
    }

    // ── 외부에서 강제 닫기 (GameOver/Clear 등에서 호출) ────

    public void ForceClose()
    {
        IsPaused = false;
        HidePanel(pausePanel);
        optionsPanelInstance?.gameObject.SetActive(false);
    }

    // ── 헬퍼 ──────────────────────────────────────────────

    void ShowPanel(CanvasGroup cg)
    {
        if (cg == null)
            return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    void HidePanel(CanvasGroup cg)
    {
        if (cg == null)
            return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}
