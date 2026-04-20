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
    [Tooltip("Shown on normal levels — returns to level select.")]
    public GameObject continueButton;

    [Tooltip("Shown on the boss level — leads to credits.")]
    public GameObject creditsButton;

    [Header("Scene Names")]
    public string levelSelectSceneName = "LevelSelect";
    public string menuSceneName        = "MainMenu";
    public string creditsSceneName     = "Credits";

    [Header("Settings")]
    [Tooltip("Seconds to wait before auto-returning to level select. Set to 0 to require button press.")]
    public float autoReturnDelay = 0f;

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
                           LevelManager.Instance.CurrentLevelType == LevelType.Boss;

        if (continueButton != null) continueButton.SetActive(!isBossLevel);
        if (creditsButton  != null) creditsButton.SetActive(isBossLevel);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetState(GameFlowState.Victory);

        Time.timeScale = 0f;

        if (!isBossLevel && autoReturnDelay > 0f)
            StartCoroutine(AutoReturnRoutine());
    }

    private System.Collections.IEnumerator AutoReturnRoutine()
    {
        yield return new WaitForSecondsRealtime(autoReturnDelay);
        ReturnToLevelSelect();
    }

    public void ReturnToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
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