using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Panels")]
    public CanvasGroup pausePanel;
    public CanvasGroup optionsPanel;

    private bool isPaused;

    void Awake()
    {
        Instance = this;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // GameOver / GameClear 가 이미 화면을 점유 중이면 무시
        if (Time.timeScale == 0f && !isPaused)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    // ── 퍼즈 토글 ─────────────────────────────────────────

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        ShowPanel(pausePanel);
        HidePanel(optionsPanel);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
    }

    // ── 버튼 콜백 ─────────────────────────────────────────

    public void OnResumeButton() => Resume();

    public void OnOptionsButton()
    {
        HidePanel(pausePanel);
        ShowPanel(optionsPanel);
    }

    public void OnQuitButton()
    {
        isPaused = false;
        Time.timeScale = 1f;
        HidePanel(pausePanel);
        HidePanel(optionsPanel);
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
        isPaused = false;
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
