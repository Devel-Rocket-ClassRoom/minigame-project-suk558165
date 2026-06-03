using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Normal,
    Shop,
    MiniBoss,
    Boss,
}

public class RoomManager : MonoBehaviour
{
    private const int TotalRooms = 10;
    private const int ShopRoom = 4;
    private const int MiniBossRoom = 7;
    private const int BossRoom = 10;

    [Header("Normal Room Pool")]
    [SerializeField]
    private GameObject[] normalRoomPrefabs;

    [Header("Special Room Prefabs")]
    [SerializeField]
    private GameObject shopRoomPrefab;

    [SerializeField]
    private GameObject miniBossRoomPrefab;

    [SerializeField]
    private GameObject bossRoomPrefab;

    [Header("Chest")]
    [SerializeField]
    private GameObject chestPrefab;

    [Header("References")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private GameClearUI gameClearUI;

    public int CurrentRoomNumber { get; private set; }
    public RoomType CurrentRoomType { get; private set; }

    public static RoomManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    private GameObject currentRoom;
    private List<int> normalRoomOrder;
    private int normalRoomCursor;
    private Portal currentPortal;

    private CameraFollow cameraFollow;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        cameraFollow = CameraFollow.Instance;
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void ResetDungeon()
    {
        StopAllCoroutines();
        if (currentRoom != null)
        {
            Destroy(currentRoom);
            currentRoom = null;
        }
        CurrentRoomNumber = 0;
        player = null;
    }

    public void StartGame()
    {
        ShuffleNormalRooms();
        StartCoroutine(LoadRoomWithFade(1, true));
    }

    public void ResumeGame(int roomNumber)
    {
        var data = SaveManager.Instance?.Data;
        if (data != null && data.savedRoomOrder.Count > 0)
        {
            normalRoomOrder = new List<int>(data.savedRoomOrder);
            normalRoomCursor = data.savedRoomCursor;
        }
        else
        {
            ShuffleNormalRooms();
        }
        StartCoroutine(LoadRoomWithFade(roomNumber, true));
    }

    void ShuffleNormalRooms()
    {
        normalRoomOrder = new List<int>();
        for (int i = 0; i < normalRoomPrefabs.Length; i++)
            normalRoomOrder.Add(i);

        for (int i = normalRoomOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (normalRoomOrder[i], normalRoomOrder[j]) = (normalRoomOrder[j], normalRoomOrder[i]);
        }

        normalRoomCursor = 0;
        SaveRoomLayout();
    }

    void SaveRoomLayout()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null)
            return;
        data.savedRoomOrder = new List<int>(normalRoomOrder);
        data.savedRoomCursor = normalRoomCursor;
    }

    int PickNormalRoomIndex()
    {
        if (normalRoomCursor >= normalRoomOrder.Count)
            ShuffleNormalRooms();

        // 증가 전에 저장 — 컨티뉴 시 이 방을 다시 로드할 때 동일한 인덱스를 집어오기 위해
        SaveRoomLayout();
        return normalRoomOrder[normalRoomCursor++];
    }

    RoomType GetRoomType(int roomNumber)
    {
        return roomNumber switch
        {
            ShopRoom => RoomType.Shop,
            MiniBossRoom => RoomType.MiniBoss,
            BossRoom => RoomType.Boss,
            _ => RoomType.Normal,
        };
    }

    GameObject GetRoomPrefab(RoomType type)
    {
        return type switch
        {
            RoomType.Shop => shopRoomPrefab,
            RoomType.MiniBoss => miniBossRoomPrefab,
            RoomType.Boss => bossRoomPrefab,
            _ => normalRoomPrefabs[PickNormalRoomIndex()],
        };
    }

    void LoadRoom(int roomNumber)
    {
        foreach (var g in WorldGold.Instances.ToArray())
            Destroy(g.gameObject);
        foreach (var p in WorldPotion.Instances.ToArray())
            Destroy(p.gameObject);

        if (currentRoom != null)
            Destroy(currentRoom);

        CurrentRoomNumber = roomNumber;
        CurrentRoomType = GetRoomType(roomNumber);

        var prefab = GetRoomPrefab(CurrentRoomType);
        currentRoom = Instantiate(prefab);

        var spawnPoint = currentRoom.GetComponentInChildren<PlayerSpawnPoint>();
        if (spawnPoint != null && player != null)
        {
            player.position = spawnPoint.transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        var boundsObj = currentRoom.transform.Find("CameraBounds");
        var cameraBounds =
            boundsObj != null
                ? boundsObj.GetComponent<PolygonCollider2D>()
                : currentRoom.GetComponentInChildren<PolygonCollider2D>();

        if (cameraFollow == null)
            cameraFollow = CameraFollow.Instance;

        if (cameraBounds != null)
            cameraFollow?.SetBoundsFromPolygon(cameraBounds);
        else
            cameraFollow?.RefreshBounds(currentRoom.transform);

        currentPortal = currentRoom.GetComponentInChildren<Portal>();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.lastLocation = "Dungeon";
            SaveManager.Instance.Data.lastRoomNumber = roomNumber;
            SaveManager.Instance.Save();
        }

        if (CurrentRoomType == RoomType.Shop)
        {
            if (currentPortal != null)
                currentPortal.SetActive(true);
            return;
        }

        var spawnMgr = currentRoom.GetComponentInChildren<SpawnManager>();
        if (spawnMgr != null)
            spawnMgr.onAllEnemiesDead += OnRoomCleared;
    }

    void OnRoomCleared()
    {
        if (CurrentRoomType == RoomType.Boss)
        {
            OnGameClear();
            return;
        }

        SaveManager.Instance?.Save();

        SpawnChest();

        if (currentPortal != null)
            currentPortal.SetActive(true);
    }

    void SpawnChest()
    {
        if (chestPrefab == null || currentRoom == null)
            return;

        var spawnPoint = currentRoom.GetComponentInChildren<ChestSpawnPoint>();
        if (spawnPoint == null)
            return;

        Instantiate(chestPrefab, spawnPoint.transform.position, Quaternion.identity);
    }

    public void GoToNextRoom()
    {
        int next = CurrentRoomNumber + 1;
        if (next > TotalRooms)
        {
            OnGameClear();
            return;
        }

        StartCoroutine(LoadRoomWithFade(next, false));
    }

    void OnGameClear()
    {
        ResetPlayerInventory();

        if (gameClearUI == null)
            gameClearUI = GameClearUI.Instance;

        if (gameClearUI != null)
            gameClearUI.Show();
    }

    void ResetPlayerInventory()
    {
        if (player == null)
            return;

        var inventory = player.GetComponentInChildren<Inventory>();
        if (inventory != null)
        {
            // 장착 장비 초기화
            for (int i = inventory.Accessories.Count - 1; i >= 0; i--)
                inventory.RemoveAccessory(i);

            // 백팩 초기화
            for (int i = inventory.Backpack.Count - 1; i >= 0; i--)
                inventory.RemoveFromBackpack(i);

            // 장착 무기 초기화
            var weaponInv = inventory.WeaponInventory;
            if (weaponInv != null)
                weaponInv.weapons.Clear();
        }

        // 세이브 데이터에서도 무기 초기화 후 저장
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.equippedWeapons.Clear();
            SaveManager.Instance.Data.lastLocation = "Village";
            SaveManager.Instance.Data.lastRoomNumber = 1;
            SaveManager.Instance.Save();
        }
    }

    IEnumerator LoadRoomWithFade(int roomNumber, bool isFirst)
    {
        // 1. 화면을 어둡게 (이전 방이 더이상 보이지 않게)
        if (!isFirst && ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        // 2. 새 방 로드 (플레이어 위치도 새 스폰포인트로 이동)
        LoadRoom(roomNumber);

        // 3. 플레이어 위치를 함수로 미리 받아서
        Vector3 spawnPos = GetPlayerWorldPosition();

        // 4. 카메라를 그 위치로 즉시 이동
        SnapCameraTo(spawnPos);

        // 5. CameraFollow가 새 위치를 반영할 시간 확보
        yield return null;

        // 6. 페이드 인 → 카메라가 새 위치에 자리 잡힌 상태에서 맵이 드러남
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn();
    }

    Vector3 GetPlayerWorldPosition()
    {
        if (player != null)
            return player.position;
        if (PlayerRef.Exists)
            return PlayerRef.Transform.position;
        return Vector3.zero;
    }

    void SnapCameraTo(Vector3 worldPos)
    {
        if (cameraFollow == null)
            cameraFollow = CameraFollow.Instance;
        cameraFollow?.ForcePosition(worldPos);
        cameraFollow?.SnapToTarget();
    }
}
