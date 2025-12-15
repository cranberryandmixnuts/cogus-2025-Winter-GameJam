using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Inspector에서 연결할 설정창 UI 객체 변수
    public GameObject settingsPanel;
    // Inspector에서 연결할 음향창 UI 객체 변수
    public GameObject soundPanel;

    // 설정창을 켜거나 끄는 함수
    public void ToggleSettingsPanel(bool state)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(state);

            if (!state && soundPanel != null)
            {
                soundPanel.SetActive(false);
            }
        }
    }

    // 음향 패널을 켜거나 끄는 함수
    public void ToggleSoundPanel(bool state)
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(state);

            if (state && settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }

    // --- 게임 종료 함수 추가 ---
    public void QuitGame()
    {
        // #if UNITY_EDITOR: 이 코드는 유니티 에디터에서 실행 중일 때만 작동합니다.
        // 에디터에서 게임을 멈추는 역할을 합니다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        // Application.Quit(): 이 코드는 빌드된 게임(exe, apk 등)에서만 실제로 게임을 종료시킵니다.
        // 유니티 에디터에서는 작동하지 않습니다.
        Application.Quit();

        // 디버깅 용도로 콘솔에 메시지를 출력하여 버튼이 눌렸는지 확인합니다.
        Debug.Log("Game Quit Requested");
    }
}