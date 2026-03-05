using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string levelSelectScene = "LevelSelect";

    public void StartNewGame()
    {
        SceneManager.LoadScene(levelSelectScene);
    }

    public void OpenSettings()
    {
        Debug.Log("Settings menu not implemented yet.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}