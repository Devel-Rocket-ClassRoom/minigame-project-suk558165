using UnityEngine;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    private GameObject titlePanel;

    private GameObject hpBar;
    private GameObject controls;

    void OnEnable()
    {
        hpBar = GameObject.Find("HpBar_Frame");
        controls = GameObject.Find("ControlsPanel");
        SetHUD(false);
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
    }

    public void OnNewGame() => GameFlowController.Instance.StartNewGame();

    public void OnContinue() => Debug.Log("Continue: 세이브 시스템 미구현");

    public void OnOptions() => Debug.Log("Options: 미구현");

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
