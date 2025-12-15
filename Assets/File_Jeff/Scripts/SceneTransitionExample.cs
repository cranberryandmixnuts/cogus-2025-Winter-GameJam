using UnityEngine;

public class SceneTransitionExample : MonoBehaviour
{
    [Header("다음으로 전환할 씬")]
    public SceneType targetScene = SceneType.Stage1Scene;

    public void LoadTargetScene()
    {
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