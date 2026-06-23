using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DashGhostEffect : MonoBehaviour
{
    [SerializeField]
    private float ghostInterval = 0.08f;

    [SerializeField]
    private float ghostLifetime = 0.15f;

    [SerializeField]
    private Color ghostColor = new Color(1f, 1f, 1f, 0.4f);

    private SpriteRenderer sr;
    private Transform visuals;
    private CancellationTokenSource _spawnCts;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        visuals = transform.Find("Visuals");
    }

    public void StartGhost()
    {
        _spawnCts?.Cancel();
        _spawnCts?.Dispose();
        _spawnCts = new CancellationTokenSource();
        SpawnGhosts(_spawnCts.Token).Forget();
    }

    public void StopGhost()
    {
        _spawnCts?.Cancel();
        _spawnCts?.Dispose();
        _spawnCts = null;
    }

    async UniTaskVoid SpawnGhosts(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SpawnOne();
            await UniTask.Delay(System.TimeSpan.FromSeconds(ghostInterval), cancellationToken: token);
        }
    }

    void SpawnOne()
    {
        if (sr == null || sr.sprite == null)
            return;

        var go = new GameObject("DashGhost");
        var ghostSr = go.AddComponent<SpriteRenderer>();
        ghostSr.sprite = sr.sprite;
        ghostSr.color = ghostColor;
        ghostSr.sortingLayerID = sr.sortingLayerID;
        ghostSr.sortingOrder = sr.sortingOrder - 1;

        go.transform.position = transform.position;

        if (visuals != null)
            go.transform.localScale = visuals.localScale;
        else
            go.transform.localScale = transform.localScale;

        FadeAndDestroy(ghostSr).Forget();
    }

    async UniTaskVoid FadeAndDestroy(SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostSr.color;

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostLifetime);
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            await UniTask.Yield();
        }

        Destroy(ghostSr.gameObject);
    }
}
