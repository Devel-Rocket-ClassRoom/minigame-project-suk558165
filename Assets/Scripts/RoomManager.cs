using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public enum RoomType
{
    Normal,
    Shop,
    MiniBoss,
    Boss
}

public class RoomManager : MonoBehaviour
{
    private const int TotalRooms = 10;
    private const int ShopRoom = 4;
    private const int MiniBossRoom = 7;
    private const int BossRoom = 10;

    [Header("Normal Room Pool")]
    [SerializeField] private GameObject[] normalRoomPrefabs;

    [Header("Special Room Prefabs")]
    [SerializeField] private GameObject shopRoomPrefab;
    [SerializeField] private GameObject miniBossRoomPrefab;
    [SerializeField] private GameObject bossRoomPrefab;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CinemachineConfiner2D confiner;

    public int CurrentRoomNumber { get; private set; }
    public RoomType CurrentRoomType { get; private set; }

    private GameObject currentRoom;
    private List<int> normalRoomOrder;
    private int normalRoomCursor;
    private Portal currentPortal;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (confiner == null)
        {
            var cmCam = FindAnyObjectByType<CinemachineCamera>();
            if (cmCam != null) confiner = cmCam.GetComponent<CinemachineConfiner2D>();
        }

        ShuffleNormalRooms();
        StartCoroutine(LoadRoomWithFade(1, true));
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
    }

    int PickNormalRoomIndex()
    {
        if (normalRoomCursor >= normalRoomOrder.Count)
            ShuffleNormalRooms();

        return normalRoomOrder[normalRoomCursor++];
    }

    RoomType GetRoomType(int roomNumber)
    {
        return roomNumber switch
        {
            ShopRoom => RoomType.Shop,
            MiniBossRoom => RoomType.MiniBoss,
            BossRoom => RoomType.Boss,
            _ => RoomType.Normal
        };
    }

    GameObject GetRoomPrefab(RoomType type)
    {
        return type switch
        {
            RoomType.Shop => shopRoomPrefab,
            RoomType.MiniBoss => miniBossRoomPrefab,
            RoomType.Boss => bossRoomPrefab,
            _ => normalRoomPrefabs[PickNormalRoomIndex()]
        };
    }

    void LoadRoom(int roomNumber)
    {
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
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (confiner != null)
        {
            var bounds = currentRoom.GetComponentInChildren<PolygonCollider2D>();
            if (bounds != null)
            {
                confiner.BoundingShape2D = bounds;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        currentPortal = currentRoom.GetComponentInChildren<Portal>();

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
        if (currentPortal != null)
            currentPortal.SetActive(true);
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
        Debug.Log("Game Clear!");
    }

    IEnumerator LoadRoomWithFade(int roomNumber, bool isFirst)
    {
        if (!isFirst && ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        LoadRoom(roomNumber);

        yield return null;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn();
    }
}
