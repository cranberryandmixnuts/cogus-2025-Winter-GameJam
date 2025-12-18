using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer Mixer;
    [SerializeField] private Slider BGM;
    [SerializeField] private Slider SFX;

    private const float MinDb = -80f;
    private const float MinLinear = 0.0001f;

    private void Awake()
    {
        SyncSliderFromMixer("BGM_Volume", BGM);
        SyncSliderFromMixer("SFX_Volume", SFX);
    }

    public void SetBGMVolume()
    {
        SetMixerVolumeFromSlider("BGM_Volume", BGM);
    }

    public void SetSFXVolume()
    {
        SetMixerVolumeFromSlider("SFX_Volume", SFX);
    }

    private void SetMixerVolumeFromSlider(string paramName, Slider slider)
    {
        float linear = slider.value;

        if (linear <= MinLinear)
            Mixer.SetFloat(paramName, MinDb);
        else
            Mixer.SetFloat(paramName, Mathf.Log10(linear) * 20f);
    }

    private void SyncSliderFromMixer(string paramName, Slider slider)
    {

        if (!Mixer.GetFloat(paramName, out float db))
            return;

        if (db <= MinDb + 0.0001f)
            slider.value = 0f;
        else
            slider.value = Mathf.Pow(10f, db / 20f);
    }
}