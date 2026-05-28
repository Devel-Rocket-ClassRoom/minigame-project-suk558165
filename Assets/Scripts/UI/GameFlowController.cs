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

    [SerializeField]
    private GameObject pauseMenuPrefab;

    [Header("데이터")]
    [SerializeField]
    private ItemDatabase itemDatabase;

    [Header("레퍼런스")]
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    private GameObject titleInstance;
    private GameObject villageInstance;
    private GameObject playerInstance;
    private GameObject pauseMenuInstance;

    void Awake()
    {
        Instance = this;
        if (uiCanvasPrefab != null)
            Instantiate(uiCanvasPrefab);

        // SaveManager 가 씬에 없으면 자동 생성
        if (SaveManager.Instance == null)
            new GameObject("SaveManager").AddComponent<SaveManager>();

        // ItemDatabase 초기화
        if (itemDatabase != null)
            itemDatabase.Init();

        // RunStats 가 씬에 없으면 자동 생성
        if (RunStats.Instance == null)
            new GameObject("RunStats").AddComponent<RunStats>();
    }

    void Start() => GoToTitle();

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // 인벤토리가 열려있으면 ESC 무시
        if (InventoryUI.IsOpen)
            return;

        // GameOver / GameClear 가 이미 화면을 점유 중이면 무시
        if (Time.timeScale == 0f && !PauseMenu.IsPaused)
            return;

        if (PauseMenu.IsPaused)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    void OpenPauseMenu()
    {
        if (pauseMenuInstance == null)
        {
            if (pauseMenuPrefab == null)
                return;
            pauseMenuInstance = Instantiate(pauseMenuPrefab);
        }
        else
        {
            pauseMenuInstance.SetActive(true);
        }

        PauseMenu.Instance.Open();
    }

    public void ClosePauseMenu()
    {
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.Close();
        if (pauseMenuInstance != null)
            pauseMenuInstance.SetActive(false);
    }

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
        // 세이브 초기화
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();

        DestroyTitle();
        SpawnPlayer();
        GoToVillage();
    }

    public void ContinueGame()
    {
        if (!HasSaveData())
            return;

        DestroyTitle();
        SpawnPlayer();
        LoadPlayerData();
        GoToVillage();
    }

    public bool HasSaveData()
    {
        return SaveManager.Instance != null
            && System.IO.File.Exists(
                System.IO.Path.Combine(
                    UnityEngine.Application.persistentDataPath,
                    "save.dat"
                )
            );
    }

    void DestroyTitle()
    {
        if (titleInstance != null)
        {
            Destroy(titleInstance);
            titleInstance = null;
        }
    }

    void SpawnPlayer()
    {
        playerInstance = Instantiate(playerPrefab);

        if (cinemachineCamera != null)
            cinemachineCamera.Follow = playerInstance.transform;

        roomManager.SetPlayer(playerInstance.transform);
    }

    void LoadPlayerData()
    {
        if (SaveManager.Instance == null)
            return;

        var data = SaveManager.Instance.Data;
        var inventory = playerInstance.GetComponentInChildren<Inventory>();

        if (inventory == null)
            return;

        // 골드 복원
        inventory.LoadGold();

        // 장착 무기 복원
        if (itemDatabase != null && data.equippedWeapons.Count > 0)
        {
            var weaponInv = inventory.WeaponInventory;
            if (weaponInv != null)
            {
                weaponInv.weapons.Clear();
                foreach (var weaponName in data.equippedWeapons)
                {
                    var weapon = itemDatabase.FindWeapon(weaponName);
                    weaponInv.weapons.Add(weapon); // null이면 빈 슬롯
                }
            }
        }
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
