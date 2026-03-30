using UnityEngine;

public enum LevelType
{
    AreaClear,
    KillAndCollect,
    Boss
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelType currentLevelType;

    private void Awake()
    {
        Instance = this;
    }

    public bool IsKillAndCollectLevel()
    {
        return currentLevelType == LevelType.KillAndCollect;
    }
}