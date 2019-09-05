using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject PauseMenuUI;
    public GameObject StartMenuUI;
    public GameObject HowToPlayUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Entrypoint.metaState == MetaState.Paused)
            {
                Entrypoint.SetMetaState(MetaState.Gameplay);
            }
            else if (Entrypoint.metaState == MetaState.Gameplay)
            {
                Entrypoint.SetMetaState(MetaState.Paused);
            }
        }
    }
}
