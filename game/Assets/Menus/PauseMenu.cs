using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject PauseMenuUI;
    public GameObject StartMenuUI;
    public GameObject HowToPlayUI;

    void Update()
    {
        if (StartMenuUI.activeInHierarchy) Entrypoint.metaState = MetaState.StartMenu;
        if (HowToPlayUI.activeInHierarchy) Entrypoint.metaState = MetaState.HowToMenu;

        if (Entrypoint.metaState != MetaState.Gameplay &&
            Entrypoint.metaState != MetaState.Paused) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Entrypoint.metaState == MetaState.Paused)
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
        Entrypoint.metaState = MetaState.Gameplay;
        Time.timeScale = 1f;
        PauseMenuUI.SetActive(false);
    }

    void Pause()
    {
        Entrypoint.metaState = MetaState.Paused;
        Time.timeScale = 0f;
        PauseMenuUI.SetActive(true);
    }
}
