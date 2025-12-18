using UnityEngine;
using UnityEngine.Audio;

public class UIManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject soundPanel;

    private bool isGamePaused = false;

    private void Update()
    {
        if (InputService.Instance.PauseDown)
        {
            TogglePause(!isGamePaused);
        }
    }

    public void TogglePause(bool shouldPause)
    {
        if (isGamePaused == shouldPause) return;
        isGamePaused = shouldPause;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(shouldPause);
        }

        if (!shouldPause && soundPanel != null)
        {
            soundPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None; // 커서를 잠그지 않음
    }

    public void OnSettingsButtonClicked()
    {
        TogglePause(!isGamePaused);
    }

    public void OnResumeButtonClicked()
    {
        TogglePause(false);
    }

    public void ToggleSoundPanel(bool state)
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(state);

            if (!state)
            {
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);
                }
            }
            else
            {
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(false);
                }
            }
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("Game Quit Requested");
    }
}