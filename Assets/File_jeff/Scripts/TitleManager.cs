using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void CutSceneStart() => SceneLoader.Instance.LoadScene(SceneType.CutScene);

    public void Stage1SceneStart() => SceneLoader.Instance.LoadScene(SceneType.Stage1Scene);
}
