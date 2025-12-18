using UnityEngine;
using UnityEngine.SceneManagement;

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
    public AudioSource SpiderPoison;
    public AudioSource SpiderBite;
    public AudioSource SpidermirrorFragments;
    public AudioSource EggBreaking;
    public AudioSource EggThrow;
    public AudioSource SnailInOut;
    public AudioSource MantisAttack;

    private AudioSource currentBGM;
    private SceneType currentSceneType = SceneType.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureBGMSource(BossBGM);
        ConfigureBGMSource(EndBGM);
        ConfigureBGMSource(NomalBGM);
        ConfigureBGMSource(CutsceneBGM);
        ConfigureBGMSource(TitleBGM);

        StopAllBGM();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        ApplyBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplyBGMForScene(newScene.name);
    }

    private void ApplyBGMForScene(string sceneName)
    {
        if (!System.Enum.TryParse(sceneName, out SceneType sceneType))
            sceneType = SceneType.None;

        if (sceneType == currentSceneType)
            return;

        currentSceneType = sceneType;

        AudioSource targetBGM = GetBGMForScene(sceneType);
        if (targetBGM == currentBGM)
            return;

        SwitchBGM(targetBGM);
    }

    private AudioSource GetBGMForScene(SceneType sceneType)
    {
        return sceneType switch
        {
            SceneType.TitleScene => TitleBGM,
            SceneType.CutScene => CutsceneBGM,
            SceneType.Stage3Scene => BossBGM,
            SceneType.EngingScene => EndBGM,
            SceneType.Stage1Scene or SceneType.Stage2Scene => NomalBGM,
            _ => NomalBGM,
        };
    }

    private void SwitchBGM(AudioSource nextBGM)
    {
        if (currentBGM != null && currentBGM.isPlaying)
            currentBGM.Stop();

        currentBGM = nextBGM;

        if (!currentBGM.isPlaying)
            currentBGM.Play();
    }

    private void ConfigureBGMSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
    }

    private void StopAllBGM()
    {
        BossBGM.Stop();
        EndBGM.Stop();
        NomalBGM.Stop();
        CutsceneBGM.Stop();
        TitleBGM.Stop();
    }
}