using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonScaleEffect
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
{
    [SerializeField]
    private float hoverScale = 1.1f;

    [SerializeField]
    private float pressScale = 0.9f;

    [SerializeField]
    private float speed = 12f;

    [SerializeField]
    private AudioClip clickSound;

    private Vector3 baseScale;
    private Vector3 targetScale;

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData _) => targetScale = baseScale * hoverScale;

    public void OnPointerExit(PointerEventData _) => targetScale = baseScale;

    public void OnPointerDown(PointerEventData _)
    {
        targetScale = baseScale * pressScale;
        AudioManager.Instance?.PlaySFX(clickSound);
    }

    public void OnPointerUp(PointerEventData _) => targetScale = baseScale * hoverScale;
}
