using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public enum SceneType
{
    None = 0,
    TitleScene,
    CutScene,
    Stage1Scene,
    Stage2Scene,
    Stage3Scene,
    EndingScene
}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("페이드 UI 연결")]
    [SerializeField]
    private Image fadeImage;

    [Header("페이드 설정")]
    [SerializeField]
    private float fadeDuration = 1.0f;

    private bool isFading = false;

    public bool IsFading
    {
        get { return isFading; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage == null)
        {
            Debug.LogError("SceneLoader: fadeImage가 Inspector에 연결되지 않았습니다.");
            return;
        }

        Color imageColor = fadeImage.color;
        imageColor.a = 0f;
        fadeImage.color = imageColor;
    }

    public void LoadScene(SceneType scene)
    {
        if (isFading) return;

        string sceneName = scene.ToString();
        if (string.IsNullOrEmpty(sceneName) || sceneName == "None")
        {
            Debug.LogError("SceneLoader: 우웩");
            return;
        }

        StartCoroutine(LoadSceneSequence(sceneName));
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        isFading = true;

        yield return StartCoroutine(Fade(1f));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(Fade(0f));

        isFading = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        Color startColor = fadeImage.color;
        Color endColor = startColor;
        endColor.a = targetAlpha;

        float time = 0;

        fadeImage.raycastTarget = (targetAlpha == 1f);

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            Color currentColor = Color.Lerp(startColor, endColor, time / fadeDuration);
            fadeImage.color = currentColor;
            yield return null;
        }

        fadeImage.color = endColor;
    }
}