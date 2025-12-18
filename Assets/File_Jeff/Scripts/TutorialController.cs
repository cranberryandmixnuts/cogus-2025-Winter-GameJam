using UnityEngine;
using DG.Tweening;

public class TutorialController : MonoBehaviour
{
    [Header("튜토리얼 UI 리스트")]
    [SerializeField] private CanvasGroup[] tutorialSteps;

    private int currentIndex = 0;
    private bool isTransitioning = false;
    private GameObject playerObj;

    void Start()
    {
        // 1. 씬 시작 시 플레이어 오브젝트를 찾아서 비활성화 (에러 발생 차단)
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.SetActive(false);
        }

        // 2. 시간 정지
        Time.timeScale = 0f;

        SetupInitialUI();
    }

    private void SetupInitialUI()
    {
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            tutorialSteps[i].alpha = (i == 0) ? 1 : 0;
            tutorialSteps[i].gameObject.SetActive(i == 0);
        }
    }

    void Update()
    {
        // 3. InputService의 JumpDown 입력을 감지
        if (InputService.Instance != null && InputService.Instance.JumpDown && !isTransitioning)
        {
            ShowNextStep();
        }
    }

    private void ShowNextStep()
    {
        if (currentIndex >= tutorialSteps.Length - 1)
        {
            FinishTutorial();
            return;
        }

        isTransitioning = true;
        int nextIndex = currentIndex + 1;

        // SetUpdate(true)를 설정해야 Time.timeScale이 0이어도 애니메이션이 작동함
        tutorialSteps[currentIndex].DOFade(0, 0.5f).SetUpdate(true).OnComplete(() => {
            tutorialSteps[currentIndex].gameObject.SetActive(false);
            tutorialSteps[nextIndex].gameObject.SetActive(true);
            tutorialSteps[nextIndex].DOFade(1, 0.5f).SetUpdate(true).OnComplete(() => {
                currentIndex = nextIndex;
                isTransitioning = false;
            });
        });
    }

    private void FinishTutorial()
    {
        isTransitioning = true;
        tutorialSteps[currentIndex].DOFade(0, 0.5f).SetUpdate(true).OnComplete(() => {
            // 4. 시간 재개
            Time.timeScale = 1f;

            // 5. 튜토리얼 종료 후 플레이어 다시 활성화
            if (playerObj != null)
            {
                playerObj.SetActive(true);
            }

            gameObject.SetActive(false);
        });
    }
}