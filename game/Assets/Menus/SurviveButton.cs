using UnityEngine;

public class SurviveButton : MonoBehaviour
{
    public void SurviveGame()
    {
        Entrypoint.metaState = MetaState.Gameplay;
        Time.timeScale = 1f;
    }
}
