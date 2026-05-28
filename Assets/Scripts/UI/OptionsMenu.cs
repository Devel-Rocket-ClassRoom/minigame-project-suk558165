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
        float master = 1f, bgm = 1f, sfx = 1f;

        if (SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.Data;
            master = data.volumeMaster;
            bgm = data.volumeBGM;
            sfx = data.volumeSFX;
        }
        else
        {
            master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            bgm = PlayerPrefs.GetFloat(KeyBGM, 1f);
            sfx = PlayerPrefs.GetFloat(KeySFX, 1f);
        }

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = master;
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = bgm;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfx;
    }

    // ── 슬라이더 OnValueChanged 콜백 ──────────────────────

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        SaveVolume(KeyMaster, value);
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeMaster = value;
    }

    public void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
        SaveVolume(KeyBGM, value);
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeBGM = value;
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        SaveVolume(KeySFX, value);
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.volumeSFX = value;
    }

    void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value); // 호환용 유지
        SaveManager.Instance?.Save();
    }

    // ── 뒤로가기 ──────────────────────────────────────────

    public void OnBackButton() => PauseMenu.Instance?.OnOptionsBackButton();
}
