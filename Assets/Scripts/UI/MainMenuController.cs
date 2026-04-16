using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string levelSelectScene = "LevelSelect";

    [Header("UI")]
    public Button  newGameButton;
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
        PlayerPrefs.SetInt("HasSave", 1);

        // Reset any leftover buffs from a previous run then generate a fresh one
        PlayerBuffManager.Instance?.ResetBuffs();
        RunManager.Instance?.GenerateRun();

        SceneManager.LoadScene(levelSelectScene);
    }

    public void LoadGame()
    {
        // Run data is in-memory only, so a Continue after closing
        // the app always starts a fresh run. A full save system
        // would serialise RunManager state here instead.
        PlayerBuffManager.Instance?.ResetBuffs();
        RunManager.Instance?.GenerateRun();

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