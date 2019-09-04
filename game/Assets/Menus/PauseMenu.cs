using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool IsGamePaused = false;
    public static bool IsStartMenuActive = false;
    public static bool IsHowToActive = false;

    public GameObject PauseMenuUI;
    public GameObject StartMenuUI;
    public GameObject HowToPlayUI;

    void Update()
    {
        IsStartMenuActive = StartMenuUI.activeInHierarchy;
        IsHowToActive = HowToPlayUI.activeInHierarchy;
        if (IsStartMenuActive) return;
        if (IsHowToActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsGamePaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void Resume()
    {
        PauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;
    }

    void Pause()
    {
        PauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;
    }
}
