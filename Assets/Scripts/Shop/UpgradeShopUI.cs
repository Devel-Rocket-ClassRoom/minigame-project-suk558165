using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마을 영구 강화창. ShopUI 패턴을 따른다.
/// 기존 골드를 재화로 사용하며, 구매는 강화 레벨업을 의미한다.
/// </summary>
public class UpgradeShopUI : MonoBehaviour
{
    [SerializeField]
    private GameObject frame;

    [SerializeField]
    private List<UpgradeSlotUI> slots = new List<UpgradeSlotUI>();

    [SerializeField]
    private TextMeshProUGUI goldText;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private TextMeshProUGUI noticeText;

    [SerializeField]
    private AudioClip buySound;

    public static bool IsOpen => openCount > 0;
    public static bool JustClosed => Time.frameCount == closedFrame;
    private static int openCount = 0;
    private static int closedFrame = -1;

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
        closeButton?.onClick.AddListener(Close);
    }

    void Start()
    {
        if (!IsOpen)
            frame?.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen || inventory == null)
            return;

        if (goldText != null)
            goldText.text = L10n.Format("ui.shop.gold", "골드: {0}", inventory.Gold);

        foreach (var slot in slots)
            slot?.Refresh(inventory.Gold);
    }

    public void Open()
    {
        inventory = Inventory.Instance;
        if (frame == null)
            return;

        var entries = MetaUpgradeConfig.Instance?.entries;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;
            if (entries != null && i < entries.Count && entries[i] != null)
                slots[i].Setup(entries[i], this);
            else
                slots[i].gameObject.SetActive(false);
        }

        if (goldText != null && inventory != null)
            goldText.text = L10n.Format("ui.shop.gold", "골드: {0}", inventory.Gold);

        frame.SetActive(true);
        openCount++;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (frame == null || !frame.activeSelf)
            return;
        frame.SetActive(false);
        openCount = Mathf.Max(0, openCount - 1);
        closedFrame = Time.frameCount;
        if (openCount == 0)
            Time.timeScale = 1f;
    }

    public void TryUpgrade(MetaUpgradeType type)
    {
        if (inventory == null)
            return;

        if (MetaUpgrades.IsMaxed(type))
        {
            ShowNotice(L10n.Get("ui.upgrade.already_max", "이미 최대 레벨입니다."));
            return;
        }

        if (!MetaUpgrades.TryUpgrade(type))
        {
            ShowNotice(L10n.Get("ui.shop.not_enough_gold", "골드가 부족합니다."));
            return;
        }

        AudioManager.Instance?.PlaySFX(buySound);

        if (goldText != null)
            goldText.text = L10n.Format("ui.shop.gold", "골드: {0}", inventory.Gold);

        foreach (var slot in slots)
            slot?.Refresh(inventory.Gold);
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
