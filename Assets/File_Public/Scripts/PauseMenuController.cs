using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    // SettingWindow (이 스크립트가 부착된 오브젝트)의 자식 컴포넌트에 접근
    [Header("자식 UI 연결 (Settings Panel과 Sound Panel)")]
    public GameObject settingsPanel;
    public GameObject soundPanel;

    // 이 스크립트가 부착된 GameObject (SettingWindow) 자체를 제어하기 위한 캐시
    private GameObject settingWindowObject;
    private bool isGamePaused = false;

    private void Awake()
    {
        // 스크립트가 부착된 SettingWindow GameObject 자체를 캐시
        settingWindowObject = gameObject;

        // 초기 상태: 비활성화
        settingWindowObject.SetActive(false);
        isGamePaused = false;

        // Settings Panel과 Sound Panel의 연결 확인
        if (settingsPanel == null || soundPanel == null)
        {
            Debug.LogError("PauseMenuController: Settings Panel 또는 Sound Panel이 Inspector에 연결되지 않았습니다.");
        }
    }

    // ESC 키 입력 감지 (InputService 사용)
    private void Update()
    {
        if (InputService.Instance != null && InputService.Instance.PauseDown)
        {
            // 현재 상태를 반전시켜 일시정지/재개 요청
            TogglePause(!isGamePaused);
        }
    }

    // 일시정지/재개 핵심 기능 (Time.timeScale 제어)
    public void TogglePause(bool shouldPause)
    {
        if (isGamePaused == shouldPause) return;
        isGamePaused = shouldPause;

        // 시간 제어: 일시정지 목표
        Time.timeScale = shouldPause ? 0f : 1f;

        // SettingWindow 전체 활성화/비활성화
        settingWindowObject.SetActive(shouldPause);

        // SettingWindow가 켜지면, 기본적으로 Settings Panel만 켜고 Sound Panel은 끕니다.
        if (shouldPause)
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        // 마우스 커서 제어
        // 일시정지 상태에서는 커서를 보이게 하고 잠금을 해제합니다.
        Cursor.visible = shouldPause;
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Locked;

        // *참고: 게임 재개 후에도 커서를 보이게 하려면 Cursor.visible = true; Cursor.lockState = CursorLockMode.None; 으로 수정하세요.*
    }

    // 버튼 이벤트 1: '설정' 버튼 클릭 또는 ESC
    public void OnSettingsButtonClicked()
    {
        TogglePause(!isGamePaused);
    }

    // 버튼 이벤트 2: '게임 재개' 버튼 클릭
    public void OnResumeButtonClicked()
    {
        TogglePause(false); // 무조건 재개
    }

    // 버튼 이벤트 3: Sound Panel 닫기 버튼이 설정창(Settings Panel)으로 돌아가도록 처리
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
}