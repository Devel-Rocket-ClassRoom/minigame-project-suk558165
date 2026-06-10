using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;
    public TextMeshProUGUI hpText;

    [Header("저체력 경고")]
    [Tooltip("이 값 이하로 떨어지면 체력 숫자를 빨간색으로 표시")]
    public float lowHpThreshold = 30f;
    public Color lowHpColor = Color.red;

    private PlayerHealth playerHealth;
    private Color defaultHpColor;
    private bool colorCaptured;

    void Update()
    {
        if (playerHealth == null)
            playerHealth = PlayerRef.Health;

        if (playerHealth == null)
            return;

        float effectiveMax = playerHealth.EffectiveMaxHp;
        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / effectiveMax);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
        {
            hpText.text =
                $"{Mathf.Max(0, Mathf.CeilToInt(playerHealth.CurrentHp))} / {Mathf.CeilToInt(effectiveMax)}";

            if (!colorCaptured)
            {
                defaultHpColor = hpText.color;
                colorCaptured = true;
            }
            hpText.color =
                playerHealth.CurrentHp <= lowHpThreshold ? lowHpColor : defaultHpColor;
        }
    }
}
