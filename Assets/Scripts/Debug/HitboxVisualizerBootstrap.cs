using UnityEngine;

public class HitboxVisualizerBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindFirstObjectByType<HitboxVisualizer>() != null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        cam.gameObject.AddComponent<HitboxVisualizer>();
    }
}
