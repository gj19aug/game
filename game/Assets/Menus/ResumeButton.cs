using UnityEngine;

public class ResumeButton : MonoBehaviour
{
    public void ResumeGame()
    {
        PauseMenu.IsGamePaused = false;
        Time.timeScale = 1f;
    }
}
