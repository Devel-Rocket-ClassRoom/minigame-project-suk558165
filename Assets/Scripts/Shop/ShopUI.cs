using System;
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

    // 씬에 있는 어떤 상점이든 하나라도 열려있으면 true
    public static bool IsOpen => openCount > 0;
    public static bool JustClosed => Time.frameCount == closedFrame;
    private static int openCount = 0;
    private static int closedFrame = -1;

    public Action<ScriptableObject> OnItemSold;
    public Action OnClosed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        openCount = 0;
        closedFrame = -1;
    }

    private Inventory inventory;
    private Coroutine noticeCoroutine;

    void Awake()
    {
        // openCount는 ResetStatics()에서만 초기화 — 여기서 리셋하면
        // 씬에 ShopUI가 여러 개일 때 이미 열린 상점 상태가 날아감
        closeButton?.onClick.AddListener(Close);
    }

    void Start()
    {
        if (!IsOpen)
            frame?.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen)
            return;

        if (goldText != null && inventory != null)
            goldText.text = $"골드: {inventory.Gold}";

        RefreshButtonStates();
    }

    void RefreshButtonStates()
    {
        if (inventory == null)
            return;
        foreach (var slot in slots)
            slot?.RefreshAffordable(inventory.Gold);
    }

    public void Open(List<ScriptableObject> items, HashSet<ScriptableObject> soldItems = null)
    {
        inventory = Inventory.Instance;
        if (frame == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;
            if (i < items.Count && items[i] != null)
            {
                slots[i].Setup(items[i], this);
                // 이미 구매한 아이템이면 품절 상태 복원
                if (soldItems != null && soldItems.Contains(items[i]))
                    slots[i].MarkSoldOut();
            }
            else
                slots[i].Clear();
        }

        if (goldText != null && inventory != null)
            goldText.text = $"골드: {inventory.Gold}";

        frame.SetActive(true);
        openCount++;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (frame != null && frame.activeSelf)
        {
            frame.SetActive(false);
            openCount = Mathf.Max(0, openCount - 1);
            closedFrame = Time.frameCount;
            if (openCount == 0)
                Time.timeScale = 1f;
            OnClosed?.Invoke();
        }
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

        OnItemSold?.Invoke(item);
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
