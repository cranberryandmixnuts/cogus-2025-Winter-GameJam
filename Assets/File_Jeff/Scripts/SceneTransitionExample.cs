using UnityEngine;
// SceneLoader.cs의 Enum에 접근

public class SceneTransitionExample : MonoBehaviour
{
    [Header("다음으로 전환할 씬")]
    public SceneType targetScene = SceneType.Stage1Scene;

    // 버튼 클릭 이벤트 등에 연결할 함수
    public void LoadTargetScene()
    {
        // 씬 로더가 존재하고, 현재 씬 로딩 중이 아닐 때만 실행
        if (SceneLoader.Instance != null && !SceneLoader.Instance.IsFading)
        {
            SceneLoader.Instance.LoadScene(targetScene);
        }
        else if (SceneLoader.Instance == null)
        {
            Debug.LogError("SceneLoader 인스턴스를 찾을 수 없습니다.");
        }
    }
}