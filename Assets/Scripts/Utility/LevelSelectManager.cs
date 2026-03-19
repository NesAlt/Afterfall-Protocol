using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    public static LevelSelectManager instance;

    public GameObject levelInfoPanel;
    public TMP_Text levelNameText;
    public TMP_Text corruptionText;

    public TMP_Text missionObjectiveText;

    private string selectedLevelScene;

    void Awake()
    {
        instance = this;
    }
    void Update()
    {
        if (levelInfoPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }
    public void SelectLevel(string levelName, string sceneName, string missionObjective, int corruption)
    {
        levelInfoPanel.SetActive(true);

        levelNameText.text = levelName;
        missionObjectiveText.text = "Current Objective: "+ missionObjective;
        corruptionText.text = "Corruption: " + corruption;

        selectedLevelScene = sceneName;
    }

    public void EnterLevel()
    {
        SceneManager.LoadScene(selectedLevelScene);
    }

    public void ClosePanel()
    {
        levelInfoPanel.SetActive(false);
    }
}