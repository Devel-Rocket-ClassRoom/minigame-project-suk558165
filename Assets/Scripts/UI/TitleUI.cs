using UnityEngine;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    private GameObject titlePanel;

    public void OnNewGame()
    {
        GameFlowController.Instance.StartNewGame();
    }

    public void OnContinue()
    {
        Debug.Log("Continue: 세이브 시스템 미구현");
    }

    public void OnOptions()
    {
        Debug.Log("Options: 미구현");
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
