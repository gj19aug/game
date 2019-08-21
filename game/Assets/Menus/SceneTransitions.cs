﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitions : MonoBehaviour
{
    public Animator transitionAnim;
    public string sceneName;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            StartCoroutine(LoadScene());
        }

        IEnumerator LoadScene()
        {
            transitionAnim.SetTrigger("end");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(sceneName);
        }

    }
    
}
 