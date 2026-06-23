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

    private Color defaultHpColor;
    private bool colorCaptured;

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateDisplay;
        // 이미 플레이어가 존재하면 현재 값으로 즉시 1회 갱신 (구독 이후 발행을 기다리지 않음).
        if (PlayerRef.Health != null)
            UpdateDisplay(PlayerRef.Health.CurrentHp, PlayerRef.Health.EffectiveMaxHp);
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateDisplay;
    }

    void UpdateDisplay(float currentHp, float effectiveMax)
    {
        float ratio = Mathf.Clamp01(currentHp / effectiveMax);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
        {
            hpText.text =
                $"{Mathf.Max(0, Mathf.CeilToInt(currentHp))} / {Mathf.CeilToInt(effectiveMax)}";

            if (!colorCaptured)
            {
                defaultHpColor = hpText.color;
                colorCaptured = true;
            }
            hpText.color = currentHp <= lowHpThreshold ? lowHpColor : defaultHpColor;
        }
    }
}
