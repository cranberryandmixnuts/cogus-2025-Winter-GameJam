using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Inspector에서 연결할 설정창 UI 객체 변수
    public GameObject settingsPanel;
    // Inspector에서 연결할 음향창 UI 객체 변수 추가
    public GameObject soundPanel;

    // 설정창을 켜거나 끄는 함수
    public void ToggleSettingsPanel(bool state)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(state);

            // NOTE: 설정창이 닫힐 때 (state가 false일 때), 
            // 혹시 모를 상황에 대비해 SoundPanel도 닫아줍니다.
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

            // NOTE: 음향 패널이 켜질 때 (state가 true일 때), 
            // Setting Panel은 자동으로 비활성화 되도록 설정할 수도 있습니다.
            // 하지만 현재 UI 구조상 Setting Panel 위에서 Sound Panel이 열리는 구조이므로
            // 이 부분은 생략하거나 디자인에 맞게 조절합니다.
            // 여기서는 Sound Panel이 열릴 때 Setting Panel이 닫히도록 해보겠습니다.
            if (state && settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }
}