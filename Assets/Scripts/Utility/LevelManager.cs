using UnityEngine;

public enum LevelType
{
    AreaClear,
    KillAndCollect,
    Boss
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }


    [Header("Editor / Standalone Fallback")]
    [Tooltip("Used when running this scene directly without a RunManager (editor testing).")]
    [SerializeField] private LevelType editorFallbackLevelType = LevelType.AreaClear;

    [Tooltip("Corruption value used when no RunManager is present.")]
    [SerializeField] private int editorFallbackCorruption = 10;

    public LevelType CurrentLevelType  { get; private set; }
    public int       CurrentCorruption { get; private set; }

    [Header("Enemy Scaling per Corruption Point (above base 10)")]
    [SerializeField] private float healthScalePerPoint = 0.03f;
    [SerializeField] private float damageScalePerPoint = 0.02f;
    [SerializeField] private float spawnScalePerPoint  = 0.01f;

    void Awake()
    {
        Instance = this;

        if (RunManager.Instance?.ActiveLevel != null)
        {
            // Normal runtime — read from RunManager
            var active        = RunManager.Instance.ActiveLevel;
            CurrentLevelType  = active.Data.levelType;
            CurrentCorruption = active.CurrentCorruption;
        }
        else
        {
            // No RunManager — use inspector fallback (editor solo testing)
            CurrentLevelType  = editorFallbackLevelType;
            CurrentCorruption = editorFallbackCorruption;
            Debug.LogWarning($"[LevelManager] No RunManager found — using editor fallback: {CurrentLevelType}, Corruption: {CurrentCorruption}");
        }

        Debug.Log($"[LevelManager] Type: {CurrentLevelType} | Corruption: {CurrentCorruption}");
    }


    public float GetEnemyHealthMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * healthScalePerPoint;

    public float GetEnemyDamageMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * damageScalePerPoint;

    public float GetEnemySpawnMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * spawnScalePerPoint;


    public bool IsKillAndCollectLevel() => CurrentLevelType == LevelType.KillAndCollect;
    public bool IsBossLevel()           => CurrentLevelType == LevelType.Boss;

    public void NotifyLevelCleared()
    {
        if (CurrentLevelType == LevelType.Boss)
            RunManager.Instance?.OnBossCleared();
        else
            RunManager.Instance?.OnLevelCleared();
    }
}