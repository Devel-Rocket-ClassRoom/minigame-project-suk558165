using UnityEngine;
using Unity.Cinemachine;

public class CameraBoundsLinker : MonoBehaviour
{
    void Start()
    {
        var confiner = GetComponent<CinemachineConfiner2D>();
        if (confiner == null) return;

        var bounds = GameObject.Find("CameraBounds");
        if (bounds != null)
        {
            confiner.BoundingShape2D = bounds.GetComponent<Collider2D>();
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
