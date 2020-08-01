using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu: MonoBehaviour {

    public void PlayGame () 
    {
        Debug.Log("xd");
        SceneManager.LoadScene (1);
    }

    public void QuitGame ()
    {
        Debug.Log ("QUIT!");
        Application.Quit(); 
    }

}