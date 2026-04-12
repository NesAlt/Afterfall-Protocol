// LevelManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Scene-level singleton.  Lives in every gameplay scene and reads the active
// level's data from the persistent RunManager on Awake.
//
// Enemy scripts should call the GetEnemy*Multiplier() methods when
// initialising their stats, e.g.:
//
//     float scaledHp = baseHp * LevelManager.Instance.GetEnemyHealthMultiplier();
//
// The scaling is linear and proportional to corruption above the base (10).
// At corruption 10 all multipliers return 1.0 (no change).
// ─────────────────────────────────────────────────────────────────────────────

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

    // ── State set from RunManager ─────────────────────────────────────────────
    public LevelType CurrentLevelType { get; private set; }
    public int       CurrentCorruption { get; private set; }

    // ── Scaling Coefficients (tweak in inspector if needed) ───────────────────
    [Header("Enemy Scaling per Corruption Point (above base 10)")]
    [Tooltip("+X% health per corruption point.")]
    [SerializeField] private float healthScalePerPoint   = 0.03f;  // +3 % / pt

    [Tooltip("+X% damage per corruption point.")]
    [SerializeField] private float damageScalePerPoint   = 0.02f;  // +2 % / pt

    [Tooltip("+X% enemy spawn count per corruption point.")]
    [SerializeField] private float spawnScalePerPoint    = 0.01f;  // +1 % / pt

    // ═════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        Instance = this;

        // Pull data from the persistent RunManager
        if (RunManager.Instance?.ActiveLevel != null)
        {
            var active          = RunManager.Instance.ActiveLevel;
            CurrentLevelType    = active.Data.levelType;
            CurrentCorruption   = active.CurrentCorruption;
        }
        else
        {
            // Fallback for running a scene standalone in the editor
            CurrentLevelType  = LevelType.AreaClear;
            CurrentCorruption = 10;
            Debug.LogWarning("[LevelManager] No active RunManager level found — using defaults.");
        }

        Debug.Log($"[LevelManager] Type: {CurrentLevelType} | Corruption: {CurrentCorruption}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Enemy Scaling
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Multiply an enemy's base max-health by this value.</summary>
    public float GetEnemyHealthMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * healthScalePerPoint;

    /// <summary>Multiply an enemy's base attack damage by this value.</summary>
    public float GetEnemyDamageMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * damageScalePerPoint;

    /// <summary>
    /// Multiply an enemy spawner's base count / wave size by this value.
    /// Round the result to the nearest integer when using it in spawner logic.
    /// </summary>
    public float GetEnemySpawnMultiplier()
        => 1f + Mathf.Max(0, CurrentCorruption - 10) * spawnScalePerPoint;

    // ═════════════════════════════════════════════════════════════════════════
    // Convenience
    // ═════════════════════════════════════════════════════════════════════════

    public bool IsKillAndCollectLevel() => CurrentLevelType == LevelType.KillAndCollect;
    public bool IsBossLevel()           => CurrentLevelType == LevelType.Boss;

    /// <summary>
    /// Call this from your objective/win-condition script once the player
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