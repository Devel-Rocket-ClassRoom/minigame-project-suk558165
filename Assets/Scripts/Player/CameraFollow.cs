using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Smoothness")]
    public float smoothTime = 0.2f;

    [Header("Offset")]
    [Tooltip("카메라가 플레이어보다 오른쪽으로 얼마나 앞서는지 (플레이어를 화면 왼쪽에 배치)")]
    public float horizontalOffset = 4f;

    [Tooltip("카메라가 플레이어보다 위로 얼마나 높은지 (플레이어를 화면 하단부에 배치)")]
    public float verticalOffset = 1.8f;

    [Header("Lookahead")]
    [Tooltip("이동 방향으로 추가로 앞을 보는 거리")]
    public float lookAheadDistance = 2f;

    [Tooltip("룩어헤드 전환 속도")]
    public float lookAheadSpeed = 3f;

    [Header("Camera")]
    public float orthographicSize = 6f;
    public float cameraZ = -10f;

    [Header("Bounds (선택)")]
    public bool useBounds = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;

    private Vector3 _velocity = Vector3.zero;
    private float _currentLookAhead = 0f;
    private float _lastFacingDir = 1f;
    private Camera _cam;
    private PlayerMovement _playerMovement;

    public void SnapToTarget()
    {
        if (target == null)
            return;
        _velocity = Vector3.zero;
        _currentLookAhead = _lastFacingDir * lookAheadDistance;
        transform.position = CalculateTargetPosition();
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

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null)
            _cam.orthographicSize = orthographicSize;

        // Cinemachine 등이 Z를 바꿨을 수 있으므로 강제 보정
        var pos = transform.position;
        pos.z = cameraZ;
        transform.position = pos;
    }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (target != null)
            _playerMovement = target.GetComponent<PlayerMovement>();

        // 시작 위치를 즉시 설정 (튀는 현상 방지)
        if (target != null)
        {
            float facingDir = GetFacingDirection();
            _currentLookAhead = facingDir * lookAheadDistance;
            transform.position = CalculateTargetPosition();
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                target = pm.transform;
                _playerMovement = pm;
                SnapToTarget();
            }
            return;
        }

        float facingDir = GetFacingDirection();
        if (facingDir != 0f)
            _lastFacingDir = facingDir;

        // 이동 방향으로 부드럽게 룩어헤드
        float targetLookAhead = _lastFacingDir * lookAheadDistance;
        _currentLookAhead = Mathf.Lerp(
            _currentLookAhead,
            targetLookAhead,
            Time.deltaTime * lookAheadSpeed
        );

        Vector3 targetPos = CalculateTargetPosition();

        if (useBounds)
        {
            float halfH = _cam != null ? _cam.orthographicSize : orthographicSize;
            float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);
            targetPos.x = Mathf.Clamp(targetPos.x, minX + halfW, maxX - halfW);
            targetPos.y = Mathf.Clamp(targetPos.y, minY + halfH, maxY - halfH);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _velocity,
            smoothTime
        );
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
        if (_playerMovement != null && _playerMovement.MoveInput != 0f)
            return Mathf.Sign(_playerMovement.MoveInput);

        // Visuals의 scaleX로 방향 확인
        if (_playerMovement != null && _playerMovement.Visuals != null)
            return _playerMovement.Visuals.localScale.x >= 0f ? 1f : -1f;

        return 0f;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_cam == null)
            _cam = GetComponent<Camera>();
        if (_cam != null)
            _cam.orthographicSize = orthographicSize;
    }
#endif
}
