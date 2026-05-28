using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    const string KeyMaster = "Vol_Master";
    const string KeyBGM = "Vol_BGM";
    const string KeySFX = "Vol_SFX";

    void OnEnable()
    {
        // 패널이 열릴 때 저장된 값 불러오기
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat(KeyMaster, 1f);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = PlayerPrefs.GetFloat(KeyBGM, 1f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(KeySFX, 1f);
    }

    // ── 슬라이더 OnValueChanged 콜백 ──────────────────────

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(KeyMaster, value);
    }

    public void OnBGMVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KeyBGM, value);
        AudioManager.Instance?.SetBGMVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KeySFX, value);
        AudioManager.Instance?.SetSFXVolume(value);
    }

    // ── 뒤로가기 ──────────────────────────────────────────

    public void OnBackButton() => PauseMenu.Instance?.OnOptionsBackButton();
}
