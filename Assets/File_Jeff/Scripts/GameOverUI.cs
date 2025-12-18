using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections; // 코루틴을 위해 추가

[RequireComponent(typeof(CanvasGroup))]
public class GameOverUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private bool isGameOverVisible = false;

    [Header("시간 설정")]
    [SerializeField] private float autoReturnDelay = 4.0f; // 타이틀로 돌아가기까지 대기 시간

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // 초기 상태 설정
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPlayerDie += ShowUI;
        }
    }

    void Update()
    {
        // UI가 떠 있는 상태에서 점프 키를 누르면 즉시 재시작
        if (isGameOverVisible && InputService.Instance != null)
        {
            if (InputService.Instance.JumpDown)
            {
                StopAllCoroutines(); // 자동 이동 예약 취소
                RestartGame();
            }
        }
    }

    private void ShowUI()
    {
        if (isGameOverVisible) return;
        isGameOverVisible = true;

        // 게임 시간 정지
        Time.timeScale = 0f;

        // UI 활성화
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // 페이드 효과 (시간 정지 중에도 작동하도록 SetUpdate(true))
        canvasGroup.DOFade(1f, 0.5f).SetUpdate(true).OnComplete(() => {
            StartCoroutine(WaitAndGoToTitle());
        });
    }

    private IEnumerator WaitAndGoToTitle()
    {
        // Time.timeScale이 0이므로 WaitForSecondsRealtime을 사용해야 실제 시간이 흐름
        yield return new WaitForSecondsRealtime(autoReturnDelay);

        Debug.Log("4초 경과: 타이틀 화면으로 이동합니다.");
        GoToTitle();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;

        // SceneLoader를 사용하여 타이틀(TitleScene)로 이동
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.TitleScene);
        }
        else
        {
            SceneManager.LoadScene("TitleScene");
        }
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPlayerDie -= ShowUI;
        }
    }
}