using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX")]
    public AudioClip itemPickUp;
    public AudioClip cashPayment;
    public AudioClip transformCoin;
    public AudioClip unlockArea;
    public AudioClip buttonClick;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        ApplyVolumes();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
    }

    public float GetSFXVolume() => sfxVolume;

    private void ApplyVolumes()
    {
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }
}
