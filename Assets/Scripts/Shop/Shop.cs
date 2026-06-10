using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [Header("상점 아이템 (최대 4개)")]
    [SerializeField]
    private List<ScriptableObject> items = new List<ScriptableObject>();

    [Header("옵션")]
    [SerializeField]
    private bool randomizeOnStart = false;

    [SerializeField]
    private bool restockOnReopen = false;

    [Header("레퍼런스")]
    [SerializeField]
    private ShopUI shopUI;

    [SerializeField]
    private Canvas parentCanvas;

    [SerializeField]
    private GameObject interactPrompt;

    private bool playerInRange = false;
    private List<ScriptableObject> activeItems = new List<ScriptableObject>();
    private readonly HashSet<ScriptableObject> soldItems = new HashSet<ScriptableObject>();

    void Start()
    {
        if (randomizeOnStart)
            activeItems = BuildRandomItems();
        else
            activeItems = new List<ScriptableObject>(items);

        SetPrompt(false);
    }

    void Update()
    {
        if (playerInRange && !ShopUI.IsOpen)
            SetPrompt(true);

        if (!playerInRange)
            return;

        var key = InputManager.Instance?.Interact ?? KeyCode.A;
        if (!Input.GetKeyDown(key))
            return;

        // A키 하나로 열기/닫기 토글 — ShopUI.Update에서 중복 처리하지 않음
        if (ShopUI.IsOpen)
            shopUI?.Close();
        else
            Open();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        playerInRange = true;
        SetPrompt(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        playerInRange = false;
        SetPrompt(false);
    }

    void SetPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }

    void EnsureShopUI()
    {
        if (shopUI == null)
            return;

        if (!shopUI.gameObject.scene.IsValid())
        {
            // Screen Space UICanvas를 찾아서 부모로 사용
            Canvas uiCanvas = null;
            foreach (var c in Canvas.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.GetComponent<UnityEngine.UI.CanvasScaler>() != null)
                {
                    uiCanvas = c;
                    break;
                }
            }

            Transform parent = uiCanvas != null ? uiCanvas.transform : (parentCanvas != null ? parentCanvas.transform : null);
            shopUI = Instantiate(shopUI, parent);

            var rt = shopUI.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var shopCanvas = shopUI.GetComponent<Canvas>();
            if (shopCanvas == null)
                shopCanvas = shopUI.gameObject.AddComponent<Canvas>();
            shopCanvas.overrideSorting = true;
            shopCanvas.sortingOrder = 100;
            if (shopUI.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                shopUI.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            shopUI.OnItemSold = item => soldItems.Add(item);
        }
    }

    void Open()
    {
        EnsureShopUI();
        if (shopUI == null)
            return;

        if (restockOnReopen)
        {
            soldItems.Clear();
            activeItems = randomizeOnStart ? BuildRandomItems() : new List<ScriptableObject>(items);
        }

        shopUI.Open(activeItems, soldItems);
    }

    List<ScriptableObject> BuildRandomItems()
    {
        if (ItemDatabase.Instance == null)
            return new List<ScriptableObject>(items);

        var pool = new List<ScriptableObject>();
        pool.AddRange(ItemDatabase.Instance.weapons);
        pool.AddRange(ItemDatabase.Instance.accessories);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var result = new List<ScriptableObject>();
        for (int i = 0; i < Mathf.Min(4, pool.Count); i++)
            result.Add(pool[i]);
        return result;
    }
}
