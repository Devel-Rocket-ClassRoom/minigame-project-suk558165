using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage minimapImage;

    [Header("카메라 설정")]
    [Tooltip("미니맵 카메라의 OrthographicSize (클수록 넓게 보임)")]
    [SerializeField] private float zoomSize = 25f;
    [SerializeField] private float cameraZ = -10f;

    [Header("RenderTexture 설정")]
    [SerializeField] private int textureWidth = 256;
    [SerializeField] private int textureHeight = 256;

    [Header("플레이어 마커")]
    [SerializeField] private RectTransform playerMarker;

    [Header("토글")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private GameObject minimapPanel;

    private Camera minimapCamera;
    private RenderTexture renderTexture;
    private Transform player;
    private bool isVisible = true;

    void Start()
    {
        CreateMinimapCamera();

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;
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

        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        minimapCamera.targetTexture = renderTexture;

        if (minimapImage != null)
            minimapImage.texture = renderTexture;
    }

    void LateUpdate()
    {
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (minimapPanel != null)
                minimapPanel.SetActive(isVisible);
            minimapCamera.enabled = isVisible;
        }

        if (!isVisible)
            return;

        minimapCamera.transform.position = new Vector3(
            player.position.x,
            player.position.y,
            cameraZ
        );
    }

    void OnDestroy()
    {
        if (minimapCamera != null)
            Destroy(minimapCamera.gameObject);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
