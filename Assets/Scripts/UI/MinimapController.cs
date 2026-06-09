using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("UI")]
    [SerializeField]
    private RawImage minimapImage;

    [Header("카메라 설정")]
    [Tooltip("미니맵 카메라의 OrthographicSize (클수록 넓게 보임)")]
    [SerializeField]
    private float zoomSize = 25f;

    [SerializeField]
    private float cameraZ = -10f;

    [Header("RenderTexture 설정")]
    [SerializeField]
    private int textureWidth = 256;

    [SerializeField]
    private int textureHeight = 256;

    [Header("플레이어 마커")]
    [SerializeField]
    private RectTransform playerMarker;

    [Header("토글")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.M;

    [SerializeField]
    private GameObject minimapPanel;

    private Camera minimapCamera;
    private RenderTexture renderTexture;
    private Transform player;
    private RoomManager roomManager;
    private bool isVisible = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateMinimapCamera();
        roomManager = RoomManager.Instance;
    }

    void CreateMinimapCamera()
    {
        var camGO = new GameObject("MinimapCamera");
        camGO.transform.SetParent(transform);
        minimapCamera = camGO.AddComponent<Camera>();

        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = zoomSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        minimapCamera.depth = -10;
        minimapCamera.cullingMask = ~(1 << 5);

        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        minimapCamera.targetTexture = renderTexture;

        if (minimapImage != null)
            minimapImage.texture = renderTexture;
    }

    void LateUpdate()
    {
        if (player == null)
            player = PlayerRef.Transform;

        if (roomManager == null)
            roomManager = RoomManager.Instance;

        bool inDungeon = roomManager != null && roomManager.CurrentRoomNumber > 0;
        bool shouldShow = isVisible && inDungeon && player != null;

        if (minimapPanel != null && minimapPanel.activeSelf != shouldShow)
            minimapPanel.SetActive(shouldShow);
        if (minimapCamera != null)
            minimapCamera.enabled = shouldShow;

        if (Input.GetKeyDown(toggleKey))
            isVisible = !isVisible;

        if (!shouldShow)
            return;

        // 플레이어를 항상 중앙에
        minimapCamera.transform.position = new Vector3(
            player.position.x,
            player.position.y,
            cameraZ
        );

        // 마커는 항상 중앙 고정
        if (playerMarker != null)
            playerMarker.anchoredPosition = Vector2.zero;
    }

    public void Hide()
    {
        if (minimapPanel != null)
            minimapPanel.SetActive(false);
        if (minimapCamera != null)
            minimapCamera.enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (minimapCamera != null)
            Destroy(minimapCamera.gameObject);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
