using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Smoothness")]
    public float smoothTime = 0.2f;

    [Header("Offset")]
    [Tooltip("카메라가 플레이어보다 오른쪽으로 얼마나 앞서는지 (플레이어를 화면 왼쪽에 배치)")]
    public float horizontalOffset = 0f;

    [Tooltip("카메라가 플레이어보다 위로 얼마나 높은지 (플레이어를 화면 하단부에 배치)")]
    public float verticalOffset = 0f;

    [Header("Lookahead")]
    [Tooltip("이동 방향으로 추가로 앞을 보는 거리")]
    public float lookAheadDistance = 0f;

    [Tooltip("룩어헤드 전환 속도")]
    public float lookAheadSpeed = 3f;

    [Header("Camera")]
    public float orthographicSize = 6f;
    public float cameraZ = -10f;

    [Header("Bounds (선택)")]
    public bool useBounds = false;

    [Tooltip("바운드 미설정 시 씬의 Tilemap에서 자동 감지")]
    public bool autoDetectBoundsFromTilemap = true;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;

    private Vector3 _velocity = Vector3.zero;
    private float _currentLookAhead = 0f;
    private float _lastFacingDir = 1f;
    private Camera _cam;
    private PlayerMovement _playerMovement;

    public static CameraFollow Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        ApplyCameraSettings();

        // Cinemachine 등이 Z를 바꿨을 수 있으므로 강제 보정
        var pos = transform.position;
        pos.z = cameraZ;
        transform.position = pos;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        TryAcquireTarget();
        RefreshBounds();

        if (target != null)
            SnapToTarget();
    }

    void LateUpdate()
    {
        if (!TryAcquireTarget())
            return;

        float facingDir = GetFacingDirection();
        if (facingDir != 0f)
            _lastFacingDir = facingDir;

        float targetLookAhead = _lastFacingDir * lookAheadDistance;
        _currentLookAhead = Mathf.Lerp(
            _currentLookAhead,
            targetLookAhead,
            Time.deltaTime * lookAheadSpeed
        );

        Vector3 targetPos = ClampToBounds(CalculateTargetPosition());
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _velocity,
            smoothTime
        );
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;
        _velocity = Vector3.zero;
        _currentLookAhead = _lastFacingDir * lookAheadDistance;
        transform.position = ClampToBounds(CalculateTargetPosition());
    }

    public void SetBoundsFromPolygon(PolygonCollider2D boundsCollider)
    {
        if (boundsCollider == null)
        {
            useBounds = false;
            return;
        }
        var b = boundsCollider.bounds;
        minX = b.min.x;
        maxX = b.max.x;
        minY = b.min.y;
        maxY = b.max.y;
        useBounds = true;
    }

    public void ClearBounds() => useBounds = false;

    public void RefreshBounds()
    {
        if (!useBounds && autoDetectBoundsFromTilemap)
            TryAutoDetectBoundsFromTilemap();
    }

    bool TryAcquireTarget()
    {
        if (target != null)
            return true;

        if (PlayerRef.Movement == null)
            return false;

        target = PlayerRef.Transform;
        _playerMovement = PlayerRef.Movement;
        return true;
    }

    void ApplyCameraSettings()
    {
        if (_cam == null)
            _cam = GetComponent<Camera>();
        if (_cam != null)
            _cam.orthographicSize = orthographicSize;
    }

    void TryAutoDetectBoundsFromTilemap()
    {
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        if (tilemaps.Length == 0)
            return;

        bool first = true;
        Bounds combined = default;
        foreach (var tm in tilemaps)
        {
            tm.CompressBounds();
            var b = tm.localBounds;
            if (b.size == Vector3.zero)
                continue;
            var worldMin = tm.transform.TransformPoint(b.min);
            var worldMax = tm.transform.TransformPoint(b.max);
            if (first)
            {
                combined = new Bounds();
                combined.SetMinMax(worldMin, worldMax);
                first = false;
            }
            else
            {
                combined.Encapsulate(worldMin);
                combined.Encapsulate(worldMax);
            }
        }

        if (first)
            return;

        minX = combined.min.x;
        maxX = combined.max.x;
        minY = combined.min.y;
        maxY = combined.max.y;
        useBounds = true;
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        if (!useBounds)
            return pos;
        float halfH = _cam != null ? _cam.orthographicSize : orthographicSize;
        float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);
        pos.x = Mathf.Clamp(pos.x, minX + halfW, maxX - halfW);
        pos.y = Mathf.Clamp(pos.y, minY + halfH, maxY - halfH);
        return pos;
    }

    Vector3 CalculateTargetPosition()
    {
        return new Vector3(
            target.position.x + horizontalOffset + _currentLookAhead,
            target.position.y + verticalOffset,
            cameraZ
        );
    }

    float GetFacingDirection()
    {
        if (_playerMovement == null)
            return 0f;

        if (_playerMovement.MoveInput != 0f)
            return Mathf.Sign(_playerMovement.MoveInput);

        if (_playerMovement.Visuals != null)
            return _playerMovement.Visuals.localScale.x >= 0f ? 1f : -1f;

        return 0f;
    }

#if UNITY_EDITOR
    void OnValidate() => ApplyCameraSettings();
#endif
}
