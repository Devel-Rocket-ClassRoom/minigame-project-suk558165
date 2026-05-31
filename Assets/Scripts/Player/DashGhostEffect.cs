using System.Collections;
using UnityEngine;

public class DashGhostEffect : MonoBehaviour
{
    [SerializeField] private float ghostInterval = 0.05f;
    [SerializeField] private float ghostLifetime = 0.25f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);

    private SpriteRenderer sr;
    private Transform visuals;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        visuals = transform.Find("Visuals");
    }

    public void StartGhost()
    {
        StartCoroutine(SpawnGhosts());
    }

    public void StopGhost()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnGhosts()
    {
        while (true)
        {
            SpawnOne();
            yield return new WaitForSeconds(ghostInterval);
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

        StartCoroutine(FadeAndDestroy(ghostSr));
    }

    IEnumerator FadeAndDestroy(SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostSr.color;

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostLifetime);
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghostSr.gameObject);
    }
}
