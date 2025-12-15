using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // 슬라이더의 Value를 가져오기 위해 필요

public class VolumeController : MonoBehaviour
{
    // 1단계에서 노출한 파라미터를 제어할 Audio Mixer (Inspector에서 연결 필요)
    public AudioMixer mainMixer;

    // BGM 슬라이더의 On Value Changed 이벤트에 연결할 함수
    public void SetBGMVolume(float sliderValue)
    {
        // 슬라이더 값(0.0001 ~ 1)을 데시벨(-80dB ~ 0dB)로 변환
        if (sliderValue <= 0.0001f)
        {
            mainMixer.SetFloat("BGM_Volume", -80f); // 0에 가까우면 -80dB로 설정 (무음)
        }
        else
        {
            // 로그 스케일 변환: Log10(값) * 20을 사용하여 자연스러운 볼륨 변화를 만듭니다.
            mainMixer.SetFloat("BGM_Volume", Mathf.Log10(sliderValue) * 20f);
        }
    }

    // VFX (효과음) 슬라이더의 On Value Changed 이벤트에 연결할 함수
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