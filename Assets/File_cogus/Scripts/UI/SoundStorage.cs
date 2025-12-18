using UnityEngine;

public class SoundStorage : MonoBehaviour
{
    public static SoundStorage Instance { get; private set; }

    [Header("BGM")]
    public AudioSource BossBGM;
    public AudioSource EndBGM;
    public AudioSource NomalBGM;
    public AudioSource CutsceneBGM;
    public AudioSource TitleBGM;

    [Header("SFX")]
    public AudioSource HealHP;
    public AudioSource Jump;
    public AudioSource SpiderDash;
    public AudioSource SpiderPoison;
    public AudioSource SpiderBite;
    public AudioSource SpidermirrorFragments;
    public AudioSource EggBreaking;
    public AudioSource EggThrow;
    public AudioSource SnailInOut;
    public AudioSource MantisAttack;

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