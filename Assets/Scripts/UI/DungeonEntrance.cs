using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEntrance : MonoBehaviour
{
    [SerializeField]
    private string dungeonSceneName = "MainTest";

    private bool _playerInRange;
    private bool _triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = false;
    }

    void Update()
    {
        if (_triggered || !_playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _triggered = true;
            if (GameFlowController.Instance != null)
                GameFlowController.Instance.EnterDungeon();
            else
                SceneManager.LoadScene(dungeonSceneName);
        }
    }
}
