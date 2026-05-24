using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class RoomManager : MonoBehaviour
{
    [Header("Room Pool")]
    [SerializeField] private GameObject[] roomPrefabs;

    [Header("Stage Settings")]
    [SerializeField] private int roomsPerStage = 5;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CinemachineConfiner2D confiner;

    public int CurrentRoomIndex { get; private set; }
    public int CurrentStage { get; private set; } = 1;

    private GameObject currentRoom;
    private List<int> roomOrder;
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

        GenerateRoomOrder();
        StartCoroutine(LoadRoomWithFade(0, true));
    }

    void GenerateRoomOrder()
    {
        roomOrder = new List<int>();
        var available = new List<int>();
        for (int i = 0; i < roomPrefabs.Length; i++)
            available.Add(i);

        for (int i = 0; i < roomsPerStage; i++)
        {
            if (available.Count == 0)
            {
                for (int j = 0; j < roomPrefabs.Length; j++)
                    available.Add(j);
            }
            int pick = Random.Range(0, available.Count);
            roomOrder.Add(available[pick]);
            available.RemoveAt(pick);
        }
    }

    void LoadRoom(int index)
    {
        if (currentRoom != null)
            Destroy(currentRoom);

        CurrentRoomIndex = index;
        int prefabIndex = roomOrder[index];
        currentRoom = Instantiate(roomPrefabs[prefabIndex]);

        // 플레이어 위치 이동
        var spawnPoint = currentRoom.GetComponentInChildren<PlayerSpawnPoint>();
        if (spawnPoint != null && player != null)
        {
            player.position = spawnPoint.transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 카메라 바운드 갱신
        if (confiner != null)
        {
            var bounds = currentRoom.GetComponentInChildren<PolygonCollider2D>();
            if (bounds != null)
            {
                confiner.BoundingShape2D = bounds;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        // 포탈 연결
        currentPortal = currentRoom.GetComponentInChildren<Portal>();

        // SpawnManager 클리어 이벤트 연결
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
        int next = CurrentRoomIndex + 1;
        if (next >= roomsPerStage)
        {
            CurrentStage++;
            GenerateRoomOrder();
            StartCoroutine(LoadRoomWithFade(0, false));
        }
        else
        {
            StartCoroutine(LoadRoomWithFade(next, false));
        }
    }

    IEnumerator LoadRoomWithFade(int index, bool isFirst)
    {
        if (!isFirst && ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        LoadRoom(index);

        yield return null;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn();
    }
}
