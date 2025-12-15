using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

// 씬 목록 Enum 정의
public enum SceneType
{
    None = 0,
    TitleScene,
    Stage1Scene,
    Stage2Scene,
    Stage3Scene
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

    // 외부에서 씬 로딩 상태를 확인하기 위한 프로퍼티 (오류 해결)
    public bool IsFading
    {
        get { return isFading; }
    }

    private void Awake()
    {
        // 싱글톤 및 DontDestroyOnLoad 적용
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

        // 초기 상태: 투명하게 설정
        Color imageColor = fadeImage.color;
        imageColor.a = 0f;
        fadeImage.color = imageColor;
    }

    // 외부 호출 함수: 씬 로드 시작
    public void LoadScene(SceneType scene)
    {
        if (isFading) return;

        string sceneName = scene.ToString();
        if (string.IsNullOrEmpty(sceneName) || sceneName == "None")
        {
            Debug.LogError("SceneLoader: 유효하지 않은 씬 타입입니다.");
            return;
        }

        StartCoroutine(LoadSceneSequence(sceneName));
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        isFading = true;

        // 페이드 아웃 (화면이 검게 변함)
        yield return StartCoroutine(Fade(1f));

        // 씬 로딩
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 페이드 인 (검은 화면이 사라짐)
        yield return StartCoroutine(Fade(0f));

        isFading = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        Color startColor = fadeImage.color;
        Color endColor = startColor;
        endColor.a = targetAlpha;

        float time = 0;

        // 페이드 중에는 이미지가 Raycast를 막아 상호작용을 차단
        fadeImage.raycastTarget = (targetAlpha == 1f);

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            Color currentColor = Color.Lerp(startColor, endColor, time / fadeDuration);
            fadeImage.color = currentColor;
            yield return null;
        }

        // 정확한 목표 알파 값으로 설정
        fadeImage.color = endColor;
    }
}