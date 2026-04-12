// RunManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Central singleton (DontDestroyOnLoad) that owns every run-level mechanic:
//
//  • Run Generation  — 3 AreaClear + 2 KillAndCollect from 5 random regions
//  • Corruption      — base 10; furthest uncleared level +20, rest +10 after
//                      each clear
//  • Buff Assignment — 1 buff for close levels, 2 for medium, 3 for far;
//                      reassigned at preview time so the panel is always accurate
//  • Turn Tracking   — distance-based; used to scale re-corruption risk
//  • Re-Corruption   — cleared levels adjacent to uncleared ones have a
//                      chance to reset based on turns taken
//  • Boss            — unlocked when all 5 run levels are cleared;
//                      drawn from one of the 5 selected regions
//
// Setup in Inspector
// ──────────────────
//  areaClearLevels[0..6]  → one LevelData asset per WorldRegion (indexed by enum)
//  killCollectLevels[0..6]
//  bossLevels[0..6]
//
// Calling convention
// ──────────────────
//  1. GenerateRun()          — at run start / main menu "Play"
//  2. PreviewLevel(level)    — called by LevelSelectManager on node click
//  3. LoadLevel(level)       — called by LevelSelectManager "Enter" button
//  4. OnLevelCleared()       — called by in-level game logic on objective complete
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // ── Inspector: Level Data Banks ──────────────────────────────────────────
    [Header("Level Data Banks  (index = (int)WorldRegion)")]
    [Tooltip("7 entries — one AreaClear LevelData per region.")]
    public LevelData[] areaClearLevels   = new LevelData[7];

    [Tooltip("7 entries — one KillAndCollect LevelData per region.")]
    public LevelData[] killCollectLevels = new LevelData[7];

    [Tooltip("7 entries — one Boss LevelData per region.")]
    public LevelData[] bossLevels        = new LevelData[7];

    // ── Inspector: Run Config ────────────────────────────────────────────────
    [Header("Run Configuration")]
    public int areaClearCount   = 3;
    public int killCollectCount = 2;

    [Header("Corruption")]
    public int baseCorruption              = 10;
    public int defaultCorruptionIncrease   = 10;
    public int furthestCorruptionIncrease  = 20;

    [Header("Re-Corruption")]
    [Range(0f, 0.5f)]
    [Tooltip("Base chance per adjacent uncleared neighbour per roll.")]
    public float baseReCorruptionChance = 0.12f;

    [Range(0f, 0.2f)]
    [Tooltip("Added to the per-neighbour chance for every extra turn taken.")]
    public float turnsReCorruptionMultiplier = 0.06f;

    [Tooltip("Flat corruption penalty added when a level is re-corrupted.")]
    public int reCorruptionCorruptionPenalty = 30;

    // ── Run State (read-only from outside) ───────────────────────────────────
    public IReadOnlyList<RunLevelState> RunLevels  => _runLevels;
    public RunLevelState BossLevel                 { get; private set; }
    public RunLevelState ActiveLevel               { get; private set; }
    public RunLevelState LastClearedLevel          { get; private set; }
    public int           TotalTurns                { get; private set; }
    public bool          BossUnlocked              => _runLevels.Count > 0 && _runLevels.All(l => l.IsCleared);

    private readonly List<RunLevelState> _runLevels = new();

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired whenever corruption, clear-states, or buff assignments change.</summary>
    public event Action OnRunStateChanged;

    /// <summary>Fired for each level that gets re-corrupted after a clear.</summary>
    public event Action<RunLevelState> OnLevelReCorrupted;

    /// <summary>Fired once when all 5 levels are cleared for the first time.</summary>
    public event Action OnBossUnlocked;

    /// <summary>Fired when the boss is cleared — run is complete.</summary>
    public event Action OnRunComplete;

    // ═════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API — Run Generation
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates a fresh run: 5 levels (3 AreaClear + 2 KillAndCollect) from
    /// 5 randomly chosen distinct regions, plus a boss level.
    /// </summary>
    public void GenerateRun()
    {
        _runLevels.Clear();
        LastClearedLevel = null;
        ActiveLevel      = null;
        TotalTurns       = 0;

        // 1. Pick 5 distinct regions
        var regions = Enum.GetValues(typeof(WorldRegion))
                          .Cast<WorldRegion>()
                          .ToList();
        Shuffle(regions);
        var selected = regions.Take(areaClearCount + killCollectCount).ToList();

        // 2. Shuffle again to randomise which regions get which level type
        Shuffle(selected);
        for (int i = 0; i < selected.Count; i++)
        {
            var region = selected[i];
            var bank   = i < areaClearCount ? areaClearLevels : killCollectLevels;
            var data   = bank[(int)region];

            if (data == null)
            {
                Debug.LogWarning($"[RunManager] Missing LevelData for {region} in bank index {i}. Skipping.");
                continue;
            }

            _runLevels.Add(new RunLevelState(data, baseCorruption));
        }

        // 3. Initial buff assignment based on geographic isolation in the set
        InitialAssignAllBuffs();

        // 4. Boss from one of the selected regions
        int bossIdx = UnityEngine.Random.Range(0, selected.Count);
        var bossData = bossLevels[(int)selected[bossIdx]];
        BossLevel = new RunLevelState(bossData, baseCorruption);
        AssignBuffsToLevel(BossLevel, 3); // Boss always rewards 3 buffs

        Debug.Log($"[RunManager] Run generated — {_runLevels.Count} levels + Boss: {BossLevel?.Data.levelName}");
        OnRunStateChanged?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API — Level Select Interaction
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by LevelSelectManager when the player clicks a node.
    /// Re-assigns buffs based on current distance, then returns a preview
    /// struct with turn cost and buff count so the UI panel can display them.
    /// </summary>
    public LevelPreview PreviewLevel(RunLevelState level)
    {
        int turns     = GetTurnDistance(LastClearedLevel, level);
        int buffCount = TurnsToBuffCount(turns);

        // Assign now so the panel shows the actual buffs the player will receive
        AssignBuffsToLevel(level, buffCount);

        return new LevelPreview
        {
            Turns     = turns,
            BuffCount = buffCount,
            // Re-corruption risk: does this move leave cleared levels with uncleared neighbours?
            HasReCorruptionRisk = turns > 1 && AnyReCorruptionRiskExists()
        };
    }

    /// <summary>Loads the selected level scene and records it as active.</summary>
    public void LoadLevel(RunLevelState level)
    {
        ActiveLevel = level;
        int turns   = GetTurnDistance(LastClearedLevel, level);
        TotalTurns += turns;
        Debug.Log($"[RunManager] Loading '{level.Data.levelName}' | +{turns} turns | Total: {TotalTurns}");
        SceneManager.LoadScene(level.Data.sceneName);
    }

    /// <summary>Loads the boss scene once all 5 levels are cleared.</summary>
    public void LoadBoss()
    {
        if (!BossUnlocked)
        {
            Debug.LogWarning("[RunManager] Cannot load boss — not all levels cleared.");
            return;
        }
        ActiveLevel = BossLevel;
        TotalTurns++;
        SceneManager.LoadScene(BossLevel.Data.sceneName);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API — In-Level Callbacks
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Call this from in-level objective logic when the player completes the level.
    /// Handles buff rewards, corruption updates, re-corruption rolls, and boss unlock.
    /// </summary>
    public void OnLevelCleared()
    {
        if (ActiveLevel == null)
        {
            Debug.LogWarning("[RunManager] OnLevelCleared called with no ActiveLevel.");
            return;
        }

        var cleared = ActiveLevel;
        cleared.ClearState = LevelClearState.Cleared;

        // Give the player their buff rewards
        PlayerBuffManager.Instance?.AddBuffs(cleared.AssignedBuffs);

        bool wasAlreadyComplete = BossUnlocked;

        // Update corruption on remaining levels
        UpdateCorruptions(cleared);

        // Roll for re-corruption on previously cleared levels
        int turnsTaken = GetTurnDistance(LastClearedLevel, cleared);
        CheckReCorruption(cleared, turnsTaken);

        LastClearedLevel = cleared;
        ActiveLevel      = null;

        if (!wasAlreadyComplete && BossUnlocked)
        {
            Debug.Log("[RunManager] All levels cleared — Boss unlocked!");
            OnBossUnlocked?.Invoke();
        }

        OnRunStateChanged?.Invoke();
    }

    /// <summary>
    /// Call this when the boss is defeated.
    /// Awards boss buffs and fires OnRunComplete.
    /// </summary>
    public void OnBossCleared()
    {
        if (BossLevel != null)
        {
            BossLevel.ClearState = LevelClearState.Cleared;
            PlayerBuffManager.Instance?.AddBuffs(BossLevel.AssignedBuffs);
        }

        Debug.Log("[RunManager] Boss cleared — run complete!");
        ActiveLevel = null;
        OnRunComplete?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Distance & Turns
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns how many turns it costs to travel from <paramref name="from"/> to
    /// <paramref name="to"/>. If <paramref name="from"/> is null (first move),
    /// returns 1.
    /// </summary>
    public int GetTurnDistance(RunLevelState from, RunLevelState to)
    {
        if (from == null || to == null) return 1;
        int dist = RegionDistanceHelper.GetDistance(from.Data.region, to.Data.region);
        return Mathf.Max(1, dist);
    }

    private int TurnsToBuffCount(int turns)
    {
        if (turns <= 1) return 1;
        if (turns <= 2) return 2;
        return 3;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Corruption
    // ═════════════════════════════════════════════════════════════════════════

    private void UpdateCorruptions(RunLevelState clearedLevel)
    {
        // Find the furthest uncleared level from the one just cleared
        RunLevelState furthest = null;
        int maxDist = -1;

        foreach (var lvl in _runLevels)
        {
            if (!lvl.NeedsClearing || lvl == clearedLevel) continue;
            int d = RegionDistanceHelper.GetDistance(clearedLevel.Data.region, lvl.Data.region);
            if (d > maxDist) { maxDist = d; furthest = lvl; }
        }

        foreach (var lvl in _runLevels)
        {
            if (!lvl.NeedsClearing || lvl == clearedLevel) continue;
            int increase = (lvl == furthest) ? furthestCorruptionIncrease : defaultCorruptionIncrease;
            lvl.CurrentCorruption += increase;
            Debug.Log($"[Corruption] {lvl.Data.levelName}: +{increase} → {lvl.CurrentCorruption}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Re-Corruption
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckReCorruption(RunLevelState clearedLevel, int turnsTaken)
    {
        // Chance per adjacent uncleared neighbour scales with turns taken
        // (longer detours leave more "exposure time" for corruption to spread)
        float chancePerNeighbour = baseReCorruptionChance + turnsReCorruptionMultiplier * (turnsTaken - 1);

        foreach (var lvl in _runLevels)
        {
            if (!lvl.IsCleared || lvl == clearedLevel) continue;

            int unclearedNeighbours = CountUnclearedNeighbours(lvl, maxDistance: 1);
            if (unclearedNeighbours == 0) continue;

            // Compound probability: 1 - (1 - p)^n
            float totalChance = 1f - Mathf.Pow(1f - chancePerNeighbour, unclearedNeighbours);

            if (UnityEngine.Random.value < totalChance)
                ApplyReCorruption(lvl);
        }
    }

    private void ApplyReCorruption(RunLevelState level)
    {
        level.ClearState        = LevelClearState.ReCorrupted;
        level.CurrentCorruption += reCorruptionCorruptionPenalty;
        level.TimesReCorrupted++;

        // Re-corrupted levels get escalating buff rewards as incentive to re-clear
        int bonusBuffs = Mathf.Clamp(level.TimesReCorrupted + 1, 2, 4);
        AssignBuffsToLevel(level, bonusBuffs);

        Debug.Log($"[Re-Corruption] {level.Data.levelName} reset! Corruption: {level.CurrentCorruption} | Re-corrupted x{level.TimesReCorrupted}");
        OnLevelReCorrupted?.Invoke(level);
    }

    private int CountUnclearedNeighbours(RunLevelState level, int maxDistance)
    {
        int count = 0;
        foreach (var other in _runLevels)
        {
            if (!other.NeedsClearing) continue;
            if (RegionDistanceHelper.GetDistance(level.Data.region, other.Data.region) <= maxDistance)
                count++;
        }
        return count;
    }

    private bool AnyReCorruptionRiskExists()
    {
        foreach (var lvl in _runLevels)
        {
            if (!lvl.IsCleared) continue;
            if (CountUnclearedNeighbours(lvl, maxDistance: 1) > 0) return true;
        }
        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Buff Assignment
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called once at run generation. Scores each level by its average geographic
    /// distance from the other 4 — more isolated = more buffs.
    /// </summary>
    private void InitialAssignAllBuffs()
    {
        for (int i = 0; i < _runLevels.Count; i++)
        {
            float totalDist = 0;
            for (int j = 0; j < _runLevels.Count; j++)
            {
                if (i == j) continue;
                totalDist += RegionDistanceHelper.GetDistance(
                    _runLevels[i].Data.region, _runLevels[j].Data.region);
            }
            float avgDist  = _runLevels.Count > 1 ? totalDist / (_runLevels.Count - 1) : 1;
            int   buffCount = avgDist <= 1.5f ? 1 : avgDist <= 2.5f ? 2 : 3;
            AssignBuffsToLevel(_runLevels[i], buffCount);
        }
    }

    private void AssignBuffsToLevel(RunLevelState level, int count)
    {
        level.AssignedBuffs.Clear();
        if (level.Data?.buffPool == null || level.Data.buffPool.Count == 0) return;

        var pool = new List<BuffReward>(level.Data.buffPool);
        Shuffle(pool);
        int take = Mathf.Min(count, pool.Count);
        for (int i = 0; i < take; i++)
            level.AssignedBuffs.Add(pool[i]);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Utility
    // ═════════════════════════════════════════════════════════════════════════

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>Data returned by RunManager.PreviewLevel — used to populate the UI panel.</summary>
public struct LevelPreview
{
    public int  Turns;
    public int  BuffCount;
    public bool HasReCorruptionRisk;
}