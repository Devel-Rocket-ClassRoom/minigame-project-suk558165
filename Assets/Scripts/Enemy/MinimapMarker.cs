using UnityEngine;

/// <summary>
/// 미니맵에만 보이는 사각형 마커를 자식으로 자동 생성하는 컴포넌트.
/// 적 프리팹에 붙여두면 메인 화면에는 영향 없이 미니맵에만 빨간 점으로 표시된다.
///
/// 동작 원리:
///  - Start에서 자식 GameObject 생성
///  - 자식의 Layer를 "MinimapMarker"로 설정
///  - Main Camera: cullingMask에서 MinimapMarker 레이어 제외 → 게임 화면엔 안 보임
///  - Minimap Camera: cullingMask에 MinimapMarker 포함 → 미니맵엔 보임
///
/// 셋업:
///  1) Edit > Project Settings > Tags and Layers 에 "MinimapMarker" 레이어 추가
///  2) Main Camera의 Culling Mask에서 MinimapMarker 체크 해제
///  3) MinimapController는 자동으로 MinimapMarker 레이어를 포함 (코드에서 처리됨)
/// </summary>
[DisallowMultipleComponent]
public class MinimapMarker : MonoBehaviour
{
    [SerializeField]
    [Tooltip("마커 색상. 적은 빨강, 보스는 짙은 빨강 등 구분 가능")]
    Color color = Color.red;

    [SerializeField]
    [Tooltip("마커 크기. 보스는 크게, 일반 적은 작게.")]
    Vector2 size = new Vector2(0.5f, 0.5f);

    [SerializeField]
    [Tooltip("자식이 속할 레이어 이름. 메인 카메라는 이 레이어를 제외, 미니맵 카메라는 포함")]
    string layerName = "MinimapMarker";

    [SerializeField]
    [Tooltip("미니맵에서 다른 요소 위에 표시되도록 큰 값 사용")]
    int sortingOrder = 1000;

    void Start()
    {
        // 자식 GameObject 생성 — 마커 본체
        var go = new GameObject("MinimapMarker");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        // 레이어 설정 — 미니맵 카메라만 렌더링
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) go.layer = layer;

        // SpriteRenderer로 단색 사각형 표시
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    // 흰색 사각형 스프라이트를 한 번만 만들어두고 모든 마커가 공유 (메모리 절약)
    static Sprite _cachedSquare;
    static Sprite CreateSquareSprite()
    {
        if (_cachedSquare != null) return _cachedSquare;
        var tex = new Texture2D(2, 2);
        var pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _cachedSquare;
    }
}
