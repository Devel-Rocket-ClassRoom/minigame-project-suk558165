using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float zoomSize = 7f;

    public float minX = float.NegativeInfinity;
    public float maxX = float.PositiveInfinity;
    public float minY = float.NegativeInfinity;
    public float maxY = float.PositiveInfinity;

    private Vector3 velocity;

    void Start()
    {
        Camera.main.orthographicSize = zoomSize;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 goal = target.position + offset;
        goal.x = Mathf.Clamp(goal.x, minX, maxX);
        goal.y = Mathf.Clamp(goal.y, minY, maxY);

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
