using UnityEngine;

public class SoundStorage : MonoBehaviour
{
    public static SoundStorage Instance { get; private set; }

    [Header("BGM")]
    AudioSource BossBGM;
    AudioSource EndBGM;
    AudioSource NomalBGM;
    AudioSource CutsceneBGM;
    AudioSource TitleBGM;

    //[Header("SFX")]

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}