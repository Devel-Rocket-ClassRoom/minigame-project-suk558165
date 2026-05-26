using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 단일 씬에서 타이틀 → 마을 → 던전 → 클리어 흐름을 관리합니다.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("UI")]
    [SerializeField]
    private GameObject titlePanel;

    [Header("마을")]
    [SerializeField]
    private GameObject villageRoot; // Village 맵 + DungeonEntrance 묶음

    [SerializeField]
    private Transform villageSpawn; // 마을 플레이어 스폰 위치

    [SerializeField]
    private PolygonCollider2D villageCamBounds; // 마을 카메라 경계

    [Header("던전")]
    [SerializeField]
    private RoomManager roomManager;

    [Header("플레이어")]
    [SerializeField]
    private GameObject player;

    [Header("카메라")]
    [SerializeField]
    private CinemachineConfiner2D confiner;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GoToTitle();
    }

    // ── 타이틀 화면 ─────────────────────────────────────
    public void GoToTitle()
    {
        if (titlePanel)
            titlePanel.SetActive(true);
        if (villageRoot)
            villageRoot.SetActive(false);
        if (player)
            player.SetActive(false);
    }

    // TitleUI.OnNewGame() 에서 호출
    public void StartNewGame()
    {
        if (titlePanel)
            titlePanel.SetActive(false);

        if (villageRoot != null)
            GoToVillage();
        else
        {
            if (player)
                player.SetActive(true);
            roomManager.StartGame();
        }
    }

    // ── 마을 ─────────────────────────────────────────────
    void GoToVillage()
    {
        villageRoot.SetActive(true);
        player.SetActive(true);

        if (villageSpawn != null)
        {
            player.transform.position = villageSpawn.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
                rb.linearVelocity = Vector2.zero;
        }

        if (confiner != null && villageCamBounds != null)
        {
            confiner.BoundingShape2D = villageCamBounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }

    // ── 던전 진입 (DungeonEntrance 에서 호출) ────────────
    public void EnterDungeon()
    {
        if (villageRoot)
            villageRoot.SetActive(false);
        roomManager.StartGame();
        // RoomManager.LoadRoom() 이 방마다 confiner 경계를 자동 갱신함
    }
}
