using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    public float drainSpeed = 3f;

    private Transform bgTransform;
    private Transform fillTransform;
    private float targetRatio = 1f;
    private float displayRatio = 1f;

    private static Sprite _whiteSprite;

    public void Init(Vector3 worldOffset, float scale = 1f)
    {
        BuildBar(worldOffset, scale);
    }

    static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }

    void BuildBar(Vector3 offset, float scale = 1f)
    {
        var sprite = GetWhiteSprite();

        var bgGo = new GameObject("HPBar_BG");
        bgGo.transform.SetParent(transform);
        bgGo.transform.localPosition = offset;
        bgGo.transform.localRotation = Quaternion.identity;
        bgGo.transform.localScale = new Vector3(1f * scale, 0.08f * scale, 1f);
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = sprite;
        bgSr.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        bgSr.sortingOrder = 10;
        bgTransform = bgGo.transform;

        var fillGo = new GameObject("HPBar_Fill");
        fillGo.transform.SetParent(bgGo.transform);
        fillGo.transform.localPosition = Vector3.zero;
        fillGo.transform.localRotation = Quaternion.identity;
        fillGo.transform.localScale = Vector3.one;
        var fillSr = fillGo.AddComponent<SpriteRenderer>();
        fillSr.sprite = sprite;
        fillSr.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        fillSr.sortingOrder = 11;
        fillTransform = fillGo.transform;

        bgGo.SetActive(false);
    }

    void Update()
    {
        if (fillTransform == null)
            return;
        displayRatio = Mathf.MoveTowards(displayRatio, targetRatio, Time.deltaTime * drainSpeed);
        fillTransform.localScale = new Vector3(displayRatio, 1f, 1f);
        fillTransform.localPosition = new Vector3((displayRatio - 1f) * 0.5f, 0f, 0f);
    }

    public void SetHealth(float current, float max)
    {
        if (fillTransform == null)
            return;
        targetRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        displayRatio = targetRatio;
        fillTransform.localScale = new Vector3(displayRatio, 1f, 1f);
        fillTransform.localPosition = new Vector3((displayRatio - 1f) * 0.5f, 0f, 0f);
        bgTransform.gameObject.SetActive(current < max && current > 0f);
    }
}
