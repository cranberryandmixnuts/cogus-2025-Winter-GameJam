using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    // SettingWindow (이 스크립트가 부착된 오브젝트)의 자식 컴포넌트에 접근
    [Header("UI 연결")]
    public GameObject settingsPanel;
    public GameObject soundPanel;
    public AudioMixer mainMixer; // 볼륨 조절을 위한 Audio Mixer

    // 이 스크립트가 부착된 GameObject (SettingWindow) 자체를 제어하기 위한 캐시
    // private GameObject settingWindowObject; // 이제 사용하지 않음
    private bool isGamePaused = false;

    private void Awake()
    {
        // settingWindowObject = gameObject; // SettingWindow 비활성화 로직 제거
        isGamePaused = false;

        if (settingsPanel == null || soundPanel == null)
        {
            Debug.LogError("PauseMenuController: Settings Panel 또는 Sound Panel이 Inspector에 연결되지 않았습니다.");
        }

        // 초기 상태: SettingWindow는 활성화 상태를 유지하고, Settings Panel만 비활성화합니다.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (soundPanel != null)
        {
            soundPanel.SetActive(false);
        }
    }

    // ESC 키 입력 감지 (InputService 사용)
    private void Update()
    {
        if (InputService.Instance != null && InputService.Instance.PauseDown)
        {
            // ESC는 무조건 열기만 합니다 (isGamePaused가 false일 때만 작동)
            if (!isGamePaused)
            {
                TogglePause(true);
            }
        }
    }

    // 일시정지/재개 핵심 기능 (Time.timeScale 제어)
    public void TogglePause(bool shouldPause)
    {
        if (isGamePaused == shouldPause) return;
        isGamePaused = shouldPause;

        // 시간 제어
        Time.timeScale = shouldPause ? 0f : 1f;

        // SettingWindow 전체 활성화/비활성화 로직 제거
        // settingWindowObject.SetActive(shouldPause); 

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(shouldPause);
        }

        // 일시정지 해제 시 Sound Panel도 닫음 (TogglePause(false)가 호출될 때)
        if (!shouldPause && soundPanel != null)
        {
            soundPanel.SetActive(false);
        }

        Cursor.visible = shouldPause;
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Locked;

        // *참고: 만약 '게임 재개' 버튼을 누른 후에도 커서를 보이게 유지해야 한다면, 
        //  shouldPause가 false일 때 Cursor.lockState = CursorLockMode.None; 으로 설정해야 합니다.*
    }

    // --- 버튼 상호작용 기능 ---
    // 1. '설정' 버튼 클릭 (열기 전용)
    public void OnSettingsButtonClicked()
    {
        if (!isGamePaused)
        {
            TogglePause(true);
        }
    }

    // 2. '게임 재개' 버튼 클릭 (닫기 전용)
    public void OnResumeButtonClicked()
    {
        TogglePause(false); // 무조건 재개 (닫기)
    }

    // 3. '음향' 버튼에 연결: Sound Panel을 열고 Settings Panel을 닫음
    public void OnSoundButtonClicked()
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(true);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // 4. Sound Panel 닫기 버튼: 설정창(Settings Panel)으로 복귀
    public void ReturnToSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        if (soundPanel != null)
        {
            soundPanel.SetActive(false);
        }
    }

    // 5. 게임 종료 기능 (Quit Button에 연결)
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("Game Quit Requested");
    }

    // 6. BGM 슬라이더에 연결
    public void SetBGMVolume(float sliderValue)
    {
        if (mainMixer == null) return;
        float volume = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        mainMixer.SetFloat("BGM_Volume", volume);
    }

    // 7. SFX/VFX 슬라이더에 연결
    public void SetSFXVolume(float sliderValue)
    {
        if (mainMixer == null) return;
        float volume = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        mainMixer.SetFloat("SFX_Volume", volume);
    }
}