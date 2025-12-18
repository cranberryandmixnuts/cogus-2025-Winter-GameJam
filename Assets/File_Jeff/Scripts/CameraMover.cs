using UnityEngine;
using DG.Tweening; // DOTween 네임스페이스 필요
using System.Collections;

public class CameraMover : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float targetY = -20f;   // 목표 Y 위치
    [SerializeField] private float duration = 5.0f;  // 이동에 걸리는 시간
    [SerializeField] private float startDelay = 0.5f; // 시작 전 대기 시간

    [Header("종료 후 이동 설정")]
    [SerializeField] private float waitTimeAfterMove = 3.0f; // 이동 완료 후 대기 시간

    void Start()
    {
        MoveCameraAndReturn();
    }

    public void MoveCameraAndReturn()
    {
        transform.DOKill();

        float initialZ = transform.position.z;

        transform.DOMoveY(targetY, duration)
            .SetDelay(startDelay)
            .SetEase(Ease.InOutQuad)
            .OnUpdate(() => {
                Vector3 pos = transform.position;
                pos.z = initialZ;
                transform.position = pos;
            })
            .OnComplete(() => {
                Debug.Log("카메라 이동 완료. 3초 대기 시작.");
                StartCoroutine(WaitAndGoToTitle());
            });
    }

    private IEnumerator WaitAndGoToTitle()
    {
        yield return new WaitForSeconds(waitTimeAfterMove);

        if (SceneLoader.Instance != null)
        {
            Debug.Log("타이틀 화면으로 이동합니다.");
            SceneLoader.Instance.LoadScene(SceneType.TitleScene);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
        }
    }
}