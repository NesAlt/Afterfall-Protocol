using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VictoryUIController : MonoBehaviour
{
    public static VictoryUIController Instance;

    [Header("Panel")]
    public GameObject victoryPanel;

    [Header("Buttons")]
    [Tooltip("Shown on non-boss levels.")]
    public GameObject mainMenuButton;

    [Tooltip("Shown on the boss level — leads to credits.")]
    public GameObject creditsButton;

    [Header("Scene Names")]
    public string menuSceneName   = "MainMenu";
    public string creditsSceneName = "Credits";

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        victoryPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        victoryPanel.SetActive(true);

        bool isBossLevel = LevelManager.Instance != null &&
                           LevelManager.Instance.currentLevelType == LevelType.Boss;

        if (mainMenuButton != null) mainMenuButton.SetActive(!isBossLevel);
        if (creditsButton  != null) creditsButton.SetActive(isBossLevel);

        Time.timeScale = 0f;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        SceneManager.LoadScene(menuSceneName);
#endif
    }

    public void GoToCredits()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(creditsSceneName);
    }
}