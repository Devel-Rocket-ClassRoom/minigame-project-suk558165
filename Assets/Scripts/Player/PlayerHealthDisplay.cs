using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("UI References (씬에서 연결)")]
    public Image fillImage;
    public TextMeshProUGUI hpText;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? FindFirstObjectByType<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth == null)
            return;

        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text =
                $"{Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp))} / {Mathf.CeilToInt(playerHealth.maxHp)}";
    }
}
