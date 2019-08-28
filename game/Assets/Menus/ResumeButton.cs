using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResumeButton : MonoBehaviour
{
    public static bool GameIsPaused;

    public void ResumeGame()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

    }
}
