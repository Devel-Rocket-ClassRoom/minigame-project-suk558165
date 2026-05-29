using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField]
    private GameObject frame;

    [SerializeField]
    private List<ShopSlotUI> slots = new List<ShopSlotUI>();

    [SerializeField]
    private TextMeshProUGUI goldText;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private TextMeshProUGUI noticeText;

    public static bool IsOpen { get; private set; }

    private Inventory inventory;
    private Coroutine noticeCoroutine;

    void Awake()
    {
        closeButton?.onClick.AddListener(Close);
    }

    void Start()
    {
        frame?.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen)
            return;

        if (goldText != null && inventory != null)
            goldText.text = $"골드: {inventory.Gold}";

        RefreshButtonStates();

        var interactKey = InputManager.Instance?.Interact ?? KeyCode.A;
        if (Input.GetKeyDown(interactKey))
            Close();
    }

    void RefreshButtonStates()
    {
        if (inventory == null)
            return;
        foreach (var slot in slots)
            slot?.RefreshAffordable(inventory.Gold);
    }

    public void Open(List<ScriptableObject> items)
    {
        inventory = FindFirstObjectByType<Inventory>();
        if (frame == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;
            if (i < items.Count && items[i] != null)
                slots[i].Setup(items[i], this);
            else
                slots[i].Clear();
        }

        if (goldText != null && inventory != null)
            goldText.text = $"골드: {inventory.Gold}";

        frame.SetActive(true);
        IsOpen = true;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        frame?.SetActive(false);
        IsOpen = false;
        Time.timeScale = 1f;
    }

    public bool TryBuy(ScriptableObject item, int price)
    {
        if (inventory == null || inventory.Gold < price)
        {
            ShowNotice("골드가 부족합니다.");
            return false;
        }

        if (!inventory.AddToBackpack(item))
        {
            ShowNotice("인벤토리가 가득 찼습니다.");
            return false;
        }

        inventory.SpendGold(price);

        if (goldText != null)
            goldText.text = $"골드: {inventory.Gold}";

        return true;
    }

    void ShowNotice(string msg)
    {
        if (noticeText == null)
            return;
        noticeText.text = msg;
        noticeText.gameObject.SetActive(true);
        if (noticeCoroutine != null)
            StopCoroutine(noticeCoroutine);
        noticeCoroutine = StartCoroutine(HideNoticeAfter(2f));
    }

    IEnumerator HideNoticeAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (noticeText != null)
            noticeText.gameObject.SetActive(false);
    }
}
