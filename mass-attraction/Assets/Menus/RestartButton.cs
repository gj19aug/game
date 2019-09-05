using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    public void RestartGame()
    {
        Entrypoint.SetMetaState(MetaState.StartMenu);
    }
}
