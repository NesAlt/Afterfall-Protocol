// RunManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Central singleton (DontDestroyOnLoad) that owns every run-level mechanic:
//
//  • Run Generation  — 3 AreaClear + 2 KillAndCollect from 5 random regions
//  • Corruption      — base 10; furthest uncleared level +20, rest +10
//  • Buff Assignment — 1 buff for close levels, 2 for medium, 3 for far
//  • Turn Tracking   — distance-based; scales re-corruption risk
//  • Re-Corruption   — cleared levels near uncleared ones can reset
//  • Boss            — unlocked when all 5 run levels are cleared
//  • Pending Buffs   — buffs are held here after a clear; LevelSelectManager
//                      shows BuffSelectionPanel, player picks one, then it
//                      gets applied via PlayerBuffManager
//  • Save / Load     — auto-saves after every level clear via RunSaveSystem
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
    public LevelData[] areaClearLevels   = new LevelData[7];
    public LevelData[] killCollectLevels = new LevelData[7];
    public LevelData[] bossLevels        = new LevelData[7];

    // ── Inspector: Run Config ────────────────────────────────────────────────
    [Header("Run Configuration")]
    public int areaClearCount   = 3;
    public int killCollectCount = 2;

    [Header("Corruption")]
    public int baseCorruption             = 10;
    public int defaultCorruptionIncrease  = 10;
    public int furthestCorruptionIncrease = 20;

    [Header("Re-Corruption")]
    [Range(0f, 0.5f)]
    public float baseReCorruptionChance      = 0.12f;
    [Range(0f, 0.2f)]
    public float turnsReCorruptionMultiplier = 0.06f;
    public int   reCorruptionCorruptionPenalty = 30;

    // ── Run State ─────────────────────────────────────────────────────────────
    public IReadOnlyList<RunLevelState> RunLevels => _runLevels;
    public RunLevelState BossLevel        { get; private set; }
    public RunLevelState ActiveLevel      { get; private set; }
    public RunLevelState LastClearedLevel { get; private set; }
    public int           TotalTurns       { get; private set; }
    public bool BossUnlocked => _runLevels.Count > 0 && _runLevels.All(l => l.IsCleared);

    /// <summary>
    /// Buffs from the last cleared level waiting for the player to choose from.
    /// Populated in OnLevelCleared(), consumed by BuffSelectionPanel.
    /// Null or empty means no pending choice.
    /// </summary>
    public List<BuffReward> PendingBuffChoices { get; private set; } = new();

    private readonly List<RunLevelState> _runLevels = new();

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action                OnRunStateChanged;
    public event Action<RunLevelState> OnLevelReCorrupted;
    public event Action                OnBossUnlocked;
    public event Action                OnRunComplete;

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
    // Run Generation
    // ═════════════════════════════════════════════════════════════════════════

    public void GenerateRun()
    {
        _runLevels.Clear();
        LastClearedLevel  = null;
        ActiveLevel       = null;
        TotalTurns        = 0;
        PendingBuffChoices.Clear();

        var regions = Enum.GetValues(typeof(WorldRegion))
                          .Cast<WorldRegion>().ToList();
        Shuffle(regions);
        var selected = regions.Take(areaClearCount + killCollectCount).ToList();

        Shuffle(selected);
        for (int i = 0; i < selected.Count; i++)
        {
            var region = selected[i];
            var bank   = i < areaClearCount ? areaClearLevels : killCollectLevels;
            var data   = bank[(int)region];
            if (data == null) { Debug.LogWarning($"[RunManager] Missing LevelData for {region}."); continue; }
            _runLevels.Add(new RunLevelState(data, baseCorruption));
        }

        InitialAssignAllBuffs();

        int bossIdx  = UnityEngine.Random.Range(0, selected.Count);
        var bossData = bossLevels[(int)selected[bossIdx]];
        BossLevel    = new RunLevelState(bossData, baseCorruption);
        AssignBuffsToLevel(BossLevel, 3);

        Debug.Log($"[RunManager] Run generated — {_runLevels.Count} levels + Boss: {BossLevel?.Data.levelName}");
        OnRunStateChanged?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Save System Integration
    // ═════════════════════════════════════════════════════════════════════════

    public void LoadFromSave(
        List<RunLevelState> levels,
        RunLevelState       boss,
        int                 totalTurns,
        int                 lastClearedIndex)
    {
        _runLevels.Clear();
        _runLevels.AddRange(levels);

        BossLevel   = boss;
        TotalTurns  = totalTurns;
        ActiveLevel = null;
        PendingBuffChoices.Clear();

        LastClearedLevel = (lastClearedIndex >= 0 && lastClearedIndex < _runLevels.Count)
            ? _runLevels[lastClearedIndex] : null;

        Debug.Log($"[RunManager] State restored from save — {_runLevels.Count} levels, turn {totalTurns}.");
        OnRunStateChanged?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Level Select Interaction
    // ═════════════════════════════════════════════════════════════════════════

    public LevelPreview PreviewLevel(RunLevelState level)
    {
        int turns     = GetTurnDistance(LastClearedLevel, level);
        int buffCount = TurnsToBuffCount(turns);
        AssignBuffsToLevel(level, buffCount);

        return new LevelPreview
        {
            Turns               = turns,
            BuffCount           = buffCount,
            HasReCorruptionRisk = turns > 1 && AnyReCorruptionRiskExists()
        };
    }

    public void LoadLevel(RunLevelState level)
    {
        ActiveLevel  = level;
        int turns    = GetTurnDistance(LastClearedLevel, level);
        TotalTurns  += turns;
        Debug.Log($"[RunManager] Loading '{level.Data.levelName}' | +{turns} turns | Total: {TotalTurns}");
        SceneManager.LoadScene(level.Data.sceneName);
    }

    public void LoadBoss()
    {
        if (!BossUnlocked) { Debug.LogWarning("[RunManager] Boss not yet unlocked."); return; }
        ActiveLevel = BossLevel;
        TotalTurns++;
        SceneManager.LoadScene(BossLevel.Data.sceneName);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // In-Level Callbacks
    // ═════════════════════════════════════════════════════════════════════════

    public void OnLevelCleared()
    {
        if (ActiveLevel == null) { Debug.LogWarning("[RunManager] OnLevelCleared with no ActiveLevel."); return; }

        var cleared = ActiveLevel;
        cleared.ClearState = LevelClearState.Cleared;

        // ── Store buffs as pending choices instead of auto-applying ──────────
        // BuffSelectionPanel on the LevelSelect screen will read these,
        // show the cards, and call PlayerBuffManager.AddBuff on the chosen one.
        PendingBuffChoices = new List<BuffReward>(cleared.AssignedBuffs);

        bool wasComplete = BossUnlocked;

        UpdateCorruptions(cleared);

        int turnsTaken = GetTurnDistance(LastClearedLevel, cleared);
        CheckReCorruption(cleared, turnsTaken);

        LastClearedLevel = cleared;
        ActiveLevel      = null;

        // Auto-save (pending buff choices are NOT saved — player must choose
        // before quitting; if they quit mid-choice they re-get the options on Continue)
        RunSaveSystem.SaveRun(this);

        if (!wasComplete && BossUnlocked)
        {
            Debug.Log("[RunManager] All levels cleared — Boss unlocked!");
            OnBossUnlocked?.Invoke();
        }

        OnRunStateChanged?.Invoke();
    }

    public void OnBossCleared()
    {
        if (BossLevel != null)
        {
            BossLevel.ClearState = LevelClearState.Cleared;
            // Boss buffs also go through selection
            PendingBuffChoices = new List<BuffReward>(BossLevel.AssignedBuffs);
        }

        RunSaveSystem.DeleteSave();
        Debug.Log("[RunManager] Boss cleared — run complete!");
        ActiveLevel = null;
        OnRunComplete?.Invoke();
    }

    /// <summary>
    /// Called by BuffSelectionPanel (via LevelSelectManager) once the player
    /// has picked their buff and it has been applied. Clears the pending list.
    /// </summary>
    public void ClearPendingBuffs()
    {
        PendingBuffChoices.Clear();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Distance & Turns
    // ═════════════════════════════════════════════════════════════════════════

    public int GetTurnDistance(RunLevelState from, RunLevelState to)
    {
        if (from == null || to == null) return 1;
        return Mathf.Max(1, RegionDistanceHelper.GetDistance(from.Data.region, to.Data.region));
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
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Re-Corruption
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckReCorruption(RunLevelState clearedLevel, int turnsTaken)
    {
        float chancePerNeighbour = baseReCorruptionChance + turnsReCorruptionMultiplier * (turnsTaken - 1);

        foreach (var lvl in _runLevels)
        {
            if (!lvl.IsCleared || lvl == clearedLevel) continue;
            int unclearedNeighbours = CountUnclearedNeighbours(lvl, maxDistance: 1);
            if (unclearedNeighbours == 0) continue;

            float totalChance = 1f - Mathf.Pow(1f - chancePerNeighbour, unclearedNeighbours);
            if (UnityEngine.Random.value < totalChance)
                ApplyReCorruption(lvl);
        }
    }

    private void ApplyReCorruption(RunLevelState level)
    {
        level.ClearState         = LevelClearState.ReCorrupted;
        level.CurrentCorruption += reCorruptionCorruptionPenalty;
        level.TimesReCorrupted++;

        int bonusBuffs = Mathf.Clamp(level.TimesReCorrupted + 1, 2, 4);
        AssignBuffsToLevel(level, bonusBuffs);

        Debug.Log($"[Re-Corruption] {level.Data.levelName} reset! Corruption: {level.CurrentCorruption}");
        OnLevelReCorrupted?.Invoke(level);
    }

    private int CountUnclearedNeighbours(RunLevelState level, int maxDistance)
    {
        int count = 0;
        foreach (var other in _runLevels)
            if (other.NeedsClearing && RegionDistanceHelper.GetDistance(level.Data.region, other.Data.region) <= maxDistance)
                count++;
        return count;
    }

    private bool AnyReCorruptionRiskExists()
    {
        foreach (var lvl in _runLevels)
            if (lvl.IsCleared && CountUnclearedNeighbours(lvl, maxDistance: 1) > 0)
                return true;
        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers — Buff Assignment
    // ═════════════════════════════════════════════════════════════════════════

    private void InitialAssignAllBuffs()
    {
        for (int i = 0; i < _runLevels.Count; i++)
        {
            float totalDist = 0;
            for (int j = 0; j < _runLevels.Count; j++)
            {
                if (i == j) continue;
                totalDist += RegionDistanceHelper.GetDistance(_runLevels[i].Data.region, _runLevels[j].Data.region);
            }
            float avgDist  = _runLevels.Count > 1 ? totalDist / (_runLevels.Count - 1) : 1;
            int   buffCount = avgDist <= 1.5f ? 1 : avgDist <= 2.5f ? 2 : 3;
            AssignBuffsToLevel(_runLevels[i], buffCount);
        }
    }

    public void AssignBuffsToLevel(RunLevelState level, int count)
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
public struct LevelPreview
{
    public int  Turns;
    public int  BuffCount;
    public bool HasReCorruptionRisk;
}