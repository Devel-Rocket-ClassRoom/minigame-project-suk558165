using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Zoom")]
    [Tooltip("카메라 시야 크기. 값이 클수록 더 넓게 보임 (기본 5).")]
    public float orthographicSize = 7f;

    public float minX = float.NegativeInfinity;
    public float maxX = float.PositiveInfinity;
    public float minY = float.NegativeInfinity;
    public float maxY = float.PositiveInfinity;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographicSize = orthographicSize;
    }

    void Start()
    {
        if (target == null)
            return;
        Vector3 snap = target.position + offset;
        snap.x = Mathf.Clamp(snap.x, minX, maxX);
        snap.y = Mathf.Clamp(snap.y, minY, maxY);
        transform.position = snap;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
