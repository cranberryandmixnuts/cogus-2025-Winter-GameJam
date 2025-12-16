using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void GameStart() => SceneLoader.Instance.LoadScene(SceneType.Stage1Scene);
}
