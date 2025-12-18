using UnityEngine;
using UnityEngine.Audio;

public class VolumeController : MonoBehaviour
{
    public AudioMixer mainMixer;

    public void SetBGMVolume(float sliderValue)
    {
        if (sliderValue <= 0.0001f)
        {
            mainMixer.SetFloat("BGM_Volume", -80f);
        }
        else
        {
            mainMixer.SetFloat("BGM_Volume", Mathf.Log10(sliderValue) * 20f);
        }
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (sliderValue <= 0.0001f)
        {
            mainMixer.SetFloat("SFX_Volume", -80f);
        }
        else
        {
            mainMixer.SetFloat("SFX_Volume", Mathf.Log10(sliderValue) * 20f);
        }
    }
}