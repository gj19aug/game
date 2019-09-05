using UnityEngine;

public class ResumeButton : MonoBehaviour
{
    public void ResumeGame()
    {
        Entrypoint.SetMetaState(MetaState.Gameplay);
    }
}
