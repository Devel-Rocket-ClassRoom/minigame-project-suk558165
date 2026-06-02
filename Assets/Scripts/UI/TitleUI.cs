using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    private GameObject titlePanel;

    [SerializeField]
    private Button continueButton;

    [SerializeField]
    private GameObject optionsPanel;

    [SerializeField]
    private GameObject newGameConfirmPanel;

    [SerializeField]
    private GameObject quitConfirmPanel;

    private GameObject hpBar;
    private GameObject controls;
    private GameObject goldDisplay;

    void Start()
    {
        // optionsPanel이 프리팹 에셋으로 연결된 경우 씬 인스턴스로 교체
        if (optionsPanel != null && !optionsPanel.scene.IsValid())
        {
            var instance = Instantiate(optionsPanel, transform.root);
            instance.name = "TitleOptionsPanel";
            instance.SetActive(false);
            optionsPanel = instance;
        }

        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    void OnEnable()
    {
        hpBar = GameObject.Find("HpBar_Frame");
        controls = GameObject.Find("ControlsPanel");
        goldDisplay = GameObject.Find("GoldDisplay");
        SetHUD(false);

        // 세이브 데이터가 없으면 컨티뉴 버튼 비활성화
        if (continueButton != null)
            continueButton.interactable = GameFlowController.Instance.HasSaveData();
    }

    void OnDisable()
    {
        SetHUD(true);
    }

    private void SetHUD(bool visible)
    {
        if (hpBar != null)
            hpBar.SetActive(visible);
        if (controls != null)
            controls.SetActive(visible);
        if (goldDisplay != null)
            goldDisplay.SetActive(visible);
    }

    public void OnNewGame()
    {
        if (GameFlowController.Instance.HasSaveData() && newGameConfirmPanel != null)
        {
            newGameConfirmPanel.SetActive(true);
            return;
        }

        GameFlowController.Instance.StartNewGame();
    }

    public void OnNewGameConfirm()
    {
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);

        GameFlowController.Instance.StartNewGame();
    }

    public void OnNewGameCancel()
    {
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);
    }

    public void OnContinue() => GameFlowController.Instance.ContinueGame();

    public void OnOptions()
    {
        EventSystem.current?.SetSelectedGameObject(null);

        if (optionsPanel == null)
            return;

        var ui = optionsPanel.GetComponent<TitleOptionsUI>();
        if (ui != null)
            ui.Show();
        else
            optionsPanel.SetActive(true);
    }

    public void OnQuit()
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
