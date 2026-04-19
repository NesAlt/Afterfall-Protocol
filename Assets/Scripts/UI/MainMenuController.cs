using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string levelSelectScene = "LevelSelect";

    [Header("UI")]
    public Button   newGameButton;
    public TMP_Text newGameText;

    void Start()
    {
        if (RunSaveSystem.HasSave())
        {
            // A run is in progress — offer to continue it
            newGameText.text = "Continue";
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(ContinueGame);
        }
        else
        {
            newGameText.text = "New Game";
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(StartNewGame);
        }
    }

    public void StartNewGame()
    {
        // Wipe any leftover save, reset buffs, generate a fresh run
        RunSaveSystem.DeleteSave();
        PlayerBuffManager.Instance?.ResetBuffs();
        RunManager.Instance?.GenerateRun();

        SceneManager.LoadScene(levelSelectScene);
    }

    public void ContinueGame()
    {
        // Restore the saved run state into RunManager
        bool loaded = RunSaveSystem.LoadRun(RunManager.Instance);

        if (!loaded)
        {
            // Save was corrupt or missing — fall back to a new run
            Debug.LogWarning("[MainMenu] Save load failed — starting a new run instead.");
            StartNewGame();
            return;
        }

        SceneManager.LoadScene(levelSelectScene);
    }

    public void OpenSettings()
    {
        Debug.Log("Settings menu not implemented yet.");
    }
    public void OpenCredits()
    {
        SceneManager.LoadScene("Credits");
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