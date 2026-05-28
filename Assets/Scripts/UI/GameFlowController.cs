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

    [SerializeField]
    private GameObject uiCanvasPrefab;

    [Header("레퍼런스")]
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    private GameObject titleInstance;
    private GameObject villageInstance;
    private GameObject playerInstance;

    void Awake()
    {
        Instance = this;
        if (uiCanvasPrefab != null)
            Instantiate(uiCanvasPrefab);

        // RunStats 가 씬에 없으면 자동 생성
        if (RunStats.Instance == null)
            new GameObject("RunStats").AddComponent<RunStats>();
    }

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

    public void ReturnToVillage()
    {
        roomManager.ResetDungeon();
        FindFirstObjectByType<GameClearUI>()?.ResetUI();
        FindFirstObjectByType<GameOverUI>()?.ResetUI();

        // 사망 후 귀환 시 플레이어 전체 상태 복구 (HP + 물리 + 애니메이터)
        if (playerInstance != null)
            playerInstance.GetComponent<PlayerController>()?.Revive();

        GoToVillage();
    }

    public void EnterDungeon()
    {
        if (villageInstance != null)
        {
            Destroy(villageInstance);
            villageInstance = null;
        }

        // ResetDungeon()에서 player 레퍼런스가 null 됐을 수 있으므로 재설정
        if (playerInstance != null)
            roomManager.SetPlayer(playerInstance.transform);

        RunStats.Instance?.StartRun();
        roomManager.StartGame();
    }
}
