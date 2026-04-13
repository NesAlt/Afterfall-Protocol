// LevelManager.cs

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

    // ── Editor Fallback ───────────────────────────────────────────────────────
    // When no RunManager exists (solo scene testing in editor), these values
    // are used instead. Set them in the Inspector on your scene's LevelManager.
    [Header("Editor / Standalone Fallback")]
    [Tooltip("Used when running this scene directly without a RunManager (editor testing).")]
    [SerializeField] private LevelType editorFallbackLevelType = LevelType.AreaClear;

    [Tooltip("Corruption value used when no RunManager is present.")]
    [SerializeField] private int editorFallbackCorruption = 10;

    // ── Runtime State ─────────────────────────────────────────────────────────
    public LevelType CurrentLevelType  { get; private set; }
    public int       CurrentCorruption { get; private set; }

    // ── Scaling Coefficients ──────────────────────────────────────────────────
    [Header("Enemy Scaling per Corruption Point (above base 10)")]
    [SerializeField] private float healthScalePerPoint = 0.03f;
    [SerializeField] private float damageScalePerPoint = 0.02f;
    [SerializeField] private float spawnScalePerPoint  = 0.01f;

    // ═════════════════════════════════════════════════════════════════════════
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

    // ═════════════════════════════════════════════════════════════════════════
    // Enemy Scaling
    // ═════════════════════════════════════════════════════════════════════════

    public float GetEnemyHealthMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * healthScalePerPoint;

    public float GetEnemyDamageMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * damageScalePerPoint;

    public float GetEnemySpawnMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * spawnScalePerPoint;

    // ═════════════════════════════════════════════════════════════════════════
    // Convenience
    // ═════════════════════════════════════════════════════════════════════════

    public bool IsKillAndCollectLevel() => CurrentLevelType == LevelType.KillAndCollect;
    public bool IsBossLevel()           => CurrentLevelType == LevelType.Boss;

    /// <summary>
    /// Call from your objective/win-condition script when the player
    /// finishes all objectives in the scene.
    /// </summary>
    public void NotifyLevelCleared()
    {
        if (CurrentLevelType == LevelType.Boss)
            RunManager.Instance?.OnBossCleared();
        else
            RunManager.Instance?.OnLevelCleared();
    }
}