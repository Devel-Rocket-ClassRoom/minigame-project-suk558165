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
    private GameObject interactPrompt;

    private bool playerInRange = false;
    private List<ScriptableObject> activeItems = new List<ScriptableObject>();

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
        if (!playerInRange || ShopUI.IsOpen)
            return;

        var key = InputManager.Instance?.Interact ?? KeyCode.A;
        if (Input.GetKeyDown(key))
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

        // shopUI가 프리팹(씬 밖)이면 인스턴스화
        if (!shopUI.gameObject.scene.IsValid())
        {
            var canvas = FindFirstObjectByType<Canvas>();
            shopUI = Instantiate(shopUI, canvas != null ? canvas.transform : null);
        }
    }

    void Open()
    {
        EnsureShopUI();
        if (shopUI == null)
            return;

        if (restockOnReopen)
        {
            if (randomizeOnStart)
                activeItems = BuildRandomItems();
            else
                activeItems = new List<ScriptableObject>(items);
        }

        shopUI.Open(activeItems);
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
