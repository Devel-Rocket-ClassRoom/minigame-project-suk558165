using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 테스트 씬 전용 매니저.
/// Inspector에서 맵 프리팹 목록을 등록하고, 런타임에 OnGUI 토글로 맵을 선택해
/// 스폰 / 강제 클리어 / 재시작을 테스트할 수 있습니다.
/// </summary>
public class MapTestManager : MonoBehaviour
{
    [System.Serializable]
    public class MapEntry
    {
        public string label;
        public GameObject prefab;

        [Tooltip("체크하면 클리어 시 GameClearUI 팝업을 띄웁니다 (보스 방 전용)")]
        public bool isBossRoom;
    }

    [Header("테스트할 맵 목록")]
    [SerializeField]
    private List<MapEntry> maps = new List<MapEntry>();

    [Header("플레이어 (없으면 스폰 포인트 무시)")]
    [SerializeField]
    private Transform player;

    // ── 런타임 상태 ──────────────────────────────────────
    private int selectedIndex = 0;
    private GameObject currentRoom;
    private SpawnManager currentSpawnMgr;

    private enum State
    {
        Idle,
        Running,
        Cleared,
    }

    private State state = State.Idle;
    private string statusMsg = "";

    // ── GUI 레이아웃 상수 ────────────────────────────────
    private const float PanelX = 10f;
    private const float PanelY = 10f;
    private const float PanelW = 220f;
    private const float ToggleH = 26f;
    private const float BtnH = 34f;
    private const float Pad = 6f;

    // ── GUI 스타일 (첫 OnGUI 호출 시 초기화) ─────────────
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle toggleStyle;
    private GUIStyle btnStyle;
    private GUIStyle statusStyle;
    private bool stylesReady;

    void InitStyles()
    {
        if (stylesReady)
            return;
        stylesReady = true;

        boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8) };

        labelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };

        toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 12 };

        btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
    }

    void OnGUI()
    {
        InitStyles();

        float panelH =
            Pad
            + 20f
            + Pad // 타이틀
            + maps.Count * (ToggleH + 2f)
            + Pad // 토글 목록
            + BtnH
            + Pad // Spawn 버튼
            + BtnH
            + Pad // Force Clear 버튼
            + BtnH
            + Pad // Destroy 버튼
            + 30f
            + Pad; // 상태 표시

        Rect panel = new Rect(PanelX, PanelY, PanelW, panelH);
        GUI.Box(panel, "", boxStyle);

        float y = PanelY + Pad;
        float x = PanelX + Pad;
        float w = PanelW - Pad * 2f;

        // ─ 타이틀 ─
        GUI.Label(new Rect(x, y, w, 20f), "[ Map Test ]", labelStyle);
        y += 20f + Pad;

        // ─ 맵 토글 목록 ─
        for (int i = 0; i < maps.Count; i++)
        {
            bool isSelected = (selectedIndex == i);
            bool next = GUI.Toggle(
                new Rect(x, y, w, ToggleH),
                isSelected,
                maps[i].label ?? $"Map {i}",
                toggleStyle
            );
            if (next && !isSelected)
                selectedIndex = i;
            y += ToggleH + 2f;
        }
        y += Pad;

        // ─ Spawn 버튼 ─
        GUI.enabled = state != State.Running;
        if (GUI.Button(new Rect(x, y, w, BtnH), "▶  Spawn", btnStyle))
            SpawnSelected();
        y += BtnH + Pad;
        GUI.enabled = true;

        // ─ Force Clear 버튼 ─
        GUI.enabled = state == State.Running;
        if (GUI.Button(new Rect(x, y, w, BtnH), "✓  Force Clear", btnStyle))
            ForceClear();
        y += BtnH + Pad;
        GUI.enabled = true;

        // ─ Destroy 버튼 ─
        GUI.enabled = state != State.Idle;
        if (GUI.Button(new Rect(x, y, w, BtnH), "✕  Destroy Room", btnStyle))
            DestroyRoom();
        y += BtnH + Pad;
        GUI.enabled = true;

        // ─ 상태 표시 ─
        Color prev = GUI.color;
        GUI.color =
            state == State.Cleared ? Color.green
            : state == State.Running ? Color.yellow
            : Color.white;
        GUI.Label(new Rect(x, y, w, 30f), statusMsg, statusStyle);
        GUI.color = prev;
    }

    void SpawnSelected()
    {
        if (maps.Count == 0 || selectedIndex >= maps.Count)
            return;
        var entry = maps[selectedIndex];
        if (entry.prefab == null)
        {
            statusMsg = "프리팹이 없습니다!";
            return;
        }

        DestroyRoom();

        currentRoom = Instantiate(entry.prefab);

        // 플레이어 스폰 포인트 배치
        if (player != null)
        {
            var sp = currentRoom.GetComponentInChildren<PlayerSpawnPoint>();
            if (sp != null)
            {
                player.position = sp.transform.position;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
        }

        // SpawnManager 훅
        currentSpawnMgr = currentRoom.GetComponentInChildren<SpawnManager>();
        if (currentSpawnMgr != null)
            currentSpawnMgr.onAllEnemiesDead += OnRoomCleared;

        state = State.Running;
        statusMsg = $"[{entry.label}] 실행 중...";
    }

    void ForceClear()
    {
        // 씬에 살아있는 모든 EnemyController / BossController / MiniBossController 제거
        foreach (var e in EnemyController.Instances.ToArray())
            e.gameObject.SetActive(false);
        foreach (var b in BossController.Instances.ToArray())
            b.gameObject.SetActive(false);
        foreach (var m in MiniBossController.Instances.ToArray())
            m.gameObject.SetActive(false);

        // SpawnManager가 있으면 콜백 직접 호출 (aliveCount 우회)
        if (currentSpawnMgr != null)
            currentSpawnMgr.onAllEnemiesDead?.Invoke();
        else
            OnRoomCleared();
    }

    void OnRoomCleared()
    {
        state = State.Cleared;
        statusMsg = "CLEARED!";
        Debug.Log("[MapTest] Room Cleared!");

        var entry = (maps.Count > 0 && selectedIndex < maps.Count) ? maps[selectedIndex] : null;
        if (entry != null && entry.isBossRoom)
            GameClearUI.Instance?.Show();
    }

    void DestroyRoom()
    {
        if (currentRoom != null)
        {
            if (currentSpawnMgr != null)
                currentSpawnMgr.onAllEnemiesDead -= OnRoomCleared;
            Destroy(currentRoom);
            currentRoom = null;
            currentSpawnMgr = null;
        }
        state = State.Idle;
        statusMsg = "대기 중";
    }
}
