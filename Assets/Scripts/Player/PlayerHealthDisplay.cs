using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Slider hpSlider;
    public Text hpText;

    void Update()
    {
        if (playerHealth == null)
            return;

        float ratio = Mathf.Clamp01(playerHealth.CurrentHp / playerHealth.maxHp);

        if (hpSlider != null)
            hpSlider.value = ratio;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHp)} / {playerHealth.maxHp}";
    }
}
