using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VictoryUIController : MonoBehaviour
{
    public GameObject victoryPanel;
    public string menuSceneName = "MainMenu";

    void Start()
    {
        victoryPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        // Debug.Log("ShowVictory called");

        victoryPanel.SetActive(true);

        // Debug.Log("ActiveSelf: " + victoryPanel.activeSelf);
        // Debug.Log("ActiveInHierarchy: " + victoryPanel.activeInHierarchy);

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
}
