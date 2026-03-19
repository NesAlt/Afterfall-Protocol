using UnityEngine;

public class LevelNode : MonoBehaviour
{
    public string levelName;
    public string sceneName;
    public string missionObjective;
    public int corruptionLevel;

    public void SelectLevel()
    {
        LevelSelectManager.instance.SelectLevel(levelName, sceneName,missionObjective, corruptionLevel);
    }
}