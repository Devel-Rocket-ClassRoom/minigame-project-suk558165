using UnityEngine;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    private GameObject titlePanel;

    void OnEnable()
    {
        SetHUD(false);
    }

    void OnDisable()
    {
        SetHUD(true);
    }

    private void SetHUD(bool visible)
    {
        var hpBar = GameObject.Find("HpBar_Frame");
        var controls = GameObject.Find("ControlsPanel");
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
