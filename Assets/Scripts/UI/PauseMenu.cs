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
    public CanvasGroup optionsPanel;

    void Awake()
    {
        Instance = this;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
    }

    // ── GameFlowController 에서 호출 ──────────────────────

    public void Open()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        ShowPanel(pausePanel);
        HidePanel(optionsPanel);
    }

    public void Close()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
    }

    // ── 버튼 콜백 ─────────────────────────────────────────

    public void OnResumeButton()
    {
        GameFlowController.Instance?.ClosePauseMenu();
    }

    public void OnOptionsButton()
    {
        HidePanel(pausePanel);
        ShowPanel(optionsPanel);
    }

    public void OnQuitButton()
    {
        GameFlowController.Instance?.ClosePauseMenu();
        GameFlowController.Instance?.GoToTitle();
    }

    public void OnOptionsBackButton()
    {
        HidePanel(optionsPanel);
        ShowPanel(pausePanel);
    }

    // ── 외부에서 강제 닫기 (GameOver/Clear 등에서 호출) ────

    public void ForceClose()
    {
        IsPaused = false;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
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
