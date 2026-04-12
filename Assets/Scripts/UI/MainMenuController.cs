using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string levelSelectScene = "LevelSelect";

    [Header("UI")]
    public Button newGameButton;
    public TMP_Text newGameText;

    void Start()
    {
        if (HasSave())
        {
            newGameText.text = "Continue";
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(LoadGame);
        }
        else
        {
            newGameText.text = "New Game";
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(StartNewGame);
        }
    }

    bool HasSave()
    {
        return PlayerPrefs.HasKey("HasSave");
    }

    public void StartNewGame()
    {
        PlayerPrefs.SetInt("HasSave", 1); // mark save exists
        SceneManager.LoadScene(levelSelectScene);
    }

    public void LoadGame()
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