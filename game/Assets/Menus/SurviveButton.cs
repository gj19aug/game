using UnityEngine;

public class SurviveButton : MonoBehaviour
{
    public void SurviveGame()
    {
        Entrypoint.SetMetaState(MetaState.Gameplay);
    }

    public void SurviveAgain()
    {
        Entrypoint.SetMetaState(MetaState.StartMenu);
    }
}
