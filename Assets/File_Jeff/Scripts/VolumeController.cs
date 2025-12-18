using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // 슬라이더 조작을 위해 추가

public class VolumeController : MonoBehaviour
{
    public AudioMixer mainMixer;

    [Header("UI 슬라이더 연결 (선택 사항)")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // 씬 시작 시 저장된 볼륨 값을 불러와서 적용 (없으면 기본값 0.75f)
        float savedBGM = PlayerPrefs.GetFloat("BGM_Value", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Value", 0.75f);

        // 1. 실제 오디오 믹서에 적용
        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);

        // 2. 현재 씬의 슬라이더 위치를 저장된 값으로 맞춤
        if (bgmSlider != null) bgmSlider.value = savedBGM;
        if (sfxSlider != null) sfxSlider.value = savedSFX;
    }

    public void SetBGMVolume(float sliderValue)
    {
        // 오디오 믹서 값 설정
        float volume = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        mainMixer.SetFloat("BGM_Volume", volume);

        // 값 저장
        PlayerPrefs.SetFloat("BGM_Value", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        // 오디오 믹서 값 설정
        float volume = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        mainMixer.SetFloat("SFX_Volume", volume);

        // 값 저장
        PlayerPrefs.SetFloat("SFX_Value", sliderValue);
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 시 확실하게 저장
        PlayerPrefs.Save();
    }
}