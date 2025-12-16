using UnityEngine;

public class ContentManager : MonoBehaviour
{
    public void GameStart() => SceneLoader.Instance.LoadScene(SceneType.Stage1Scene);
}
