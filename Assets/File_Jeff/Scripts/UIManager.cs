using UnityEngine;
using UnityEngine.Audio;

public class UIManager : MonoBehaviour
{
    // --- Inspector 연결 필드 ---
    public GameObject settingsPanel;
    public GameObject soundPanel;
    public AudioMixer mainMixer; // 볼륨 조절을 위한 Audio Mixer

    // --- 내부 상태 변수 ---
    private bool isGamePaused = false;

    // 새로운 입력 시스템을 사용한 ESC 키 입력 감지
    private void Update()
    {
        // InputService의 싱글톤 인스턴스가 존재하고, PauseDown 액션이 눌렸을 때
        if (InputService.Instance != null && InputService.Instance.PauseDown)
        {
            // 일시정지 상태를 반전시켜 호출
            TogglePause(!isGamePaused);
        }
    }

    // 일시정지/재개 핵심 기능 (Time.timeScale 제어)
    public void TogglePause(bool shouldPause)
    {
        if (isGamePaused == shouldPause) return;
        isGamePaused = shouldPause;

        // UI 표시 제어: 설정창을 일시정지 상태와 동기화
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(shouldPause);
        }

        // 일시정지 해제 시 Sound Panel도 닫음
        if (!shouldPause && soundPanel != null)
        {
            soundPanel.SetActive(false);
        }

        // 마우스 커서 제어 (수정된 부분)
        // 설정창(shouldPause)이 켜지든 꺼지든, UI가 활성 상태면 커서는 보여야 함.
        // true: 설정창 켜짐 -> 커서 보임, 잠금 해제 (UI 상호작용 가능)
        // false: 설정창 꺼짐 -> 커서 보임, 잠금 해제 (게임 재개 버튼 클릭 후에도 마우스가 보임)
        Cursor.visible = true; // 커서는 항상 보이게 설정 (UI 상호작용을 위해)
        Cursor.lockState = CursorLockMode.None; // 커서를 잠그지 않음

        // *만약 플레이어가 커서를 숨기고 잠가야 하는 캐릭터라면, 
        //  shouldPause가 false일 때만 Cursor.visible = false, Cursor.lockState = CursorLockMode.Locked; 를 설정해야 합니다.*
    }

    // 'Setting Button'에 연결 (설정창 토글)
    public void OnSettingsButtonClicked()
    {
        // 현재 일시정지 상태를 반전시킵니다.
        TogglePause(!isGamePaused);
    }

    // '게임 재개' 버튼에 연결
    public void OnResumeButtonClicked()
    {
        TogglePause(false); // 무조건 재개
    }

    // '음향' 버튼과 Sound Panel 닫기 버튼에 연결
    public void ToggleSoundPanel(bool state)
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(state);

            // --- 핵심 수정 부분: 음향 패널이 닫힐 때 ---
            if (!state) // 음향 패널을 닫는 경우
            {
                // 설정 패널을 다시 활성화하여 설정 -> 음향 -> 설정으로 복귀하도록 처리
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);
                }
            }
            else // 음향 패널을 여는 경우
            {
                // 설정 패널을 닫음
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(false);
                }
            }
        }
    }

    // 게임 종료 기능
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("Game Quit Requested");
    }

    // BGM 슬라이더에 연결
    public void SetBGMVolume(float sliderValue)
    {
        if (mainMixer == null) return;
        if (sliderValue <= 0.0001f)
        {
            mainMixer.SetFloat("BGM_Volume", -80f);
        }
        else
        {
            mainMixer.SetFloat("BGM_Volume", Mathf.Log10(sliderValue) * 20f);
        }
    }

    // SFX/VFX 슬라이더에 연결
    public void SetSFXVolume(float sliderValue)
    {
        if (mainMixer == null) return;
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