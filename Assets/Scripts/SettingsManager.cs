using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.value = AudioManager.Instance.GetSFXVolume();
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }
    }

    private void OnDestroy()
    {
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }
}
