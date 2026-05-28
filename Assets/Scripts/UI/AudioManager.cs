using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        float master = PlayerPrefs.GetFloat("Vol_Master", 1f);
        float bgm = PlayerPrefs.GetFloat("Vol_BGM", 1f);
        float sfx = PlayerPrefs.GetFloat("Vol_SFX", 1f);

        AudioListener.volume = master;
        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
    }

    public void SetBGMVolume(float value)
    {
        if (bgmSource != null)
            bgmSource.volume = value;
    }

    public void SetSFXVolume(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = value;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }
}
