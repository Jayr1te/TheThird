using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{

    public void ChangeToScene(int sceneToChange)
    {
        SceneManager.LoadScene(sceneToChange);
    }
    public void Doquit()
    {
        Debug.Log("has quit the game");
        Application.Quit();
    }
}
