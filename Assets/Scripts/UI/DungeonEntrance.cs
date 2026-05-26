using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEntrance : MonoBehaviour
{
    [SerializeField]
    private string dungeonSceneName = "MainTest";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.EnterDungeon();
        else
            SceneManager.LoadScene(dungeonSceneName);
    }
}
