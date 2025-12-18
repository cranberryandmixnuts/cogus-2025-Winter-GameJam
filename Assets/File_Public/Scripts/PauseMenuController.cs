using UnityEngine;
using UnityEngine.Audio;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject settingsPanel;
    public GameObject soundPanel;
    public AudioMixer mainMixer; 

    private bool isGamePaused = false;

    private void Awake()
    {
        isGamePaused = false;

        if (settingsPanel == null || soundPanel == null)
        {
            Debug.LogError("PauseMenuController: Settings Panel 또는 Sound Panel이 Inspector에 연결되지 않았습니다.");
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (soundPanel != null)
        {
            soundPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (InputService.Instance != null && InputService.Instance.PauseDown)
        {
            // ESC는 무조건 열기만 함 
            if (!isGamePaused)
            {
                TogglePause(true);
            }
        }
    }

    public void TogglePause(bool shouldPause)
    {
        if (isGamePaused == shouldPause) return;
        isGamePaused = shouldPause;

        Time.timeScale = shouldPause ? 0f : 1f;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(shouldPause);
        }

        if (!shouldPause && soundPanel != null)
        {
            soundPanel.SetActive(false);
        }

        Cursor.visible = shouldPause;
    }

    public void OnSettingsButtonClicked()
    {
        if (!isGamePaused)
        {
            TogglePause(true);
        }
    }

    public void OnResumeButtonClicked()
    {
        TogglePause(false);
    }

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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("Game Quit Requested");
    }
}