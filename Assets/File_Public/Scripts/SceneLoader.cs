using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // Image 컴포넌트 접근을 위해 추가

public class SceneLoader : MonoBehaviour
{
    // 씬 로더를 위한 싱글톤 인스턴스
    public static SceneLoader Instance { get; private set; }

    // CanvasGroup과 Image는 이 오브젝트의 자식에 있으므로,
    // 스크립트가 직접 찾아서 사용하도록 필드를 private으로 변경합니다.
    private CanvasGroup fadeCanvasGroup;
    private Image blackScreenImage;

    [Header("페이드 설정")]
    [SerializeField]
    private float fadeDuration = 1.0f;

    private bool isFading = false;

    private void Awake()
    {
        // 1. 싱글톤 패턴 적용
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

        // 2. 필요한 컴포넌트 자동 탐색 및 초기화 (통합 형태의 핵심)
        fadeCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        blackScreenImage = GetComponentInChildren<Image>(true);

        // 컴포넌트가 제대로 연결되었는지 확인
        if (fadeCanvasGroup == null || blackScreenImage == null)
        {
            Debug.LogError("SceneLoader에 CanvasGroup 또는 Image 컴포넌트가 부족합니다. 자식에 UI 설정이 완료되었는지 확인하세요.");
            return;
        }

        // 3. 초기 상태 설정 (화면은 투명하고 상호작용 불가)
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    // --- 외부 호출 함수 (이하 변경 없음) ---
    public void LoadScene(SceneType scene)
    {
        if (isFading) return;

        string sceneName = scene.ToString();
        StartCoroutine(LoadSceneSequence(sceneName));
    }

    // --- 씬 로딩 코루틴 (변경 없음) ---
    private IEnumerator LoadSceneSequence(string sceneName)
    {
        isFading = true;

        // 1. 페이드 아웃 (화면이 검게 변함)
        yield return StartCoroutine(Fade(1f));

        // 2. 씬 로딩
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. 페이드 인 (검은 화면이 사라짐)
        yield return StartCoroutine(Fade(0f));

        isFading = false;
    }

    // --- 페이드 코루틴 (변경 없음) ---
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        // 상호작용 차단/허용 설정
        fadeCanvasGroup.blocksRaycasts = (targetAlpha == 1f);

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}