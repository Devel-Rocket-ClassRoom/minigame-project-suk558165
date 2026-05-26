using Unity.Cinemachine;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField]
    private GameObject titlePrefab;

    [SerializeField]
    private GameObject villagePrefab;

    [SerializeField]
    private GameObject playerPrefab;

    [Header("레퍼런스")]
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    private GameObject titleInstance;
    private GameObject villageInstance;
    private GameObject playerInstance;

    void Awake() => Instance = this;

    void Start() => GoToTitle();

    public void GoToTitle()
    {
        roomManager.ResetDungeon();

        if (villageInstance != null)
        {
            Destroy(villageInstance);
            villageInstance = null;
        }
        if (playerInstance != null)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }

        FindFirstObjectByType<GameOverUI>()?.ResetUI();
        FindFirstObjectByType<GameClearUI>()?.ResetUI();

        titleInstance = Instantiate(titlePrefab);
    }

    public void StartNewGame()
    {
        if (titleInstance != null)
        {
            Destroy(titleInstance);
            titleInstance = null;
        }

        playerInstance = Instantiate(playerPrefab);

        if (cinemachineCamera != null)
            cinemachineCamera.Follow = playerInstance.transform;

        roomManager.SetPlayer(playerInstance.transform);
        GoToVillage();
    }

    void GoToVillage()
    {
        villageInstance = Instantiate(villagePrefab);

        var spawn = villageInstance.GetComponentInChildren<PlayerSpawnPoint>();
        if (spawn != null)
        {
            playerInstance.transform.position = spawn.transform.position;
            var rb = playerInstance.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        var confiner = cinemachineCamera?.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            var bounds = villageInstance
                .transform.Find("CameraBounds")
                ?.GetComponent<PolygonCollider2D>();
            if (bounds != null)
            {
                confiner.BoundingShape2D = bounds;
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }

    public void EnterDungeon()
    {
        if (villageInstance != null)
        {
            Destroy(villageInstance);
            villageInstance = null;
        }
        roomManager.StartGame();
    }
}
