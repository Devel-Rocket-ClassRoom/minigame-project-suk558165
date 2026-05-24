using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Village";

    public void OnNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnContinue()
    {
        // TODO: 세이브 데이터 로드 후 씬 전환
        Debug.Log("Continue - 아직 세이브 시스템 미구현");
    }

    public void OnOptions()
    {
        // TODO: 옵션 패널 열기
        Debug.Log("Options - 아직 미구현");
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
