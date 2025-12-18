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

    private AudioSource currentBGM;

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

    public void PlayBGM(BGMType type)
    {
        if (currentBGM != null)
            currentBGM.Stop();

        switch (type)
        {
            case BGMType.Title: currentBGM = TitleBGM; break;
            case BGMType.Cutscene: currentBGM = CutsceneBGM; break;
            case BGMType.Normal: currentBGM = NomalBGM; break;
            case BGMType.Boss: currentBGM = BossBGM; break;
            case BGMType.End: currentBGM = EndBGM; break;
        }

        if (currentBGM != null)
        {
            currentBGM.loop = true;
            currentBGM.Play();
        }
    }
}

public enum BGMType
{
    Title,
    Cutscene,
    Normal,
    Boss,
    End
}