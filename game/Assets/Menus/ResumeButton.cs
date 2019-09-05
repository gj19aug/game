using UnityEngine;

public class ResumeButton : MonoBehaviour
{
    public void ResumeGame()
    {
        Entrypoint.metaState = MetaState.Gameplay;
        Time.timeScale = 1f;
    }
}
