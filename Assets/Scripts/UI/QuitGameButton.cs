using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for changing scenes

public class SceneNavigation : MonoBehaviour
{
    /// <summary>
    /// Loads the Main Menu scene by name or index.
    /// </summary>
    public void GoToMainMenu()
    {
        // Replace "MainMenu" with the exact name of your scene in the Build Settings
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Closes the game or exits play mode
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}