using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;
    public TextMeshProUGUI hpText;

    private PlayerHealth playerHealth;

    void Update()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
            return;

        float effectiveMax = playerHealth.EffectiveMaxHp;
        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / effectiveMax);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text =
                $"{Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp))} / {Mathf.CeilToInt(effectiveMax)}";
    }
}
