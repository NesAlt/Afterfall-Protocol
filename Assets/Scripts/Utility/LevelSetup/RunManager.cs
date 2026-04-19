using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Level Data Banks  (index = (int)WorldRegion)")]
    public LevelData[] areaClearLevels   = new LevelData[7];
    public LevelData[] killCollectLevels = new LevelData[7];
    public LevelData[] bossLevels        = new LevelData[7];

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

    public IReadOnlyList<RunLevelState> RunLevels => _runLevels;
    public RunLevelState BossLevel        { get; private set; }
    public RunLevelState ActiveLevel      { get; private set; }
    public RunLevelState LastClearedLevel { get; private set; }
    public int           TotalTurns       { get; private set; }
    public bool BossUnlocked => _runLevels.Count > 0 && _runLevels.All(l => l.IsCleared);

    public List<BuffReward> PendingBuffChoices { get; private set; } = new();

    private readonly List<RunLevelState> _runLevels = new();

    public event Action                OnRunStateChanged;
    public event Action<RunLevelState> OnLevelReCorrupted;
    public event Action                OnBossUnlocked;
    public event Action                OnRunComplete;


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


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
        if (level?.Data == null)
        {
            Debug.LogError("[RunManager] LoadLevel called with null level or LevelData.");
            return;
        }

        if (string.IsNullOrEmpty(level.Data.sceneName))
        {
            Debug.LogError($"[RunManager] LevelData '{level.Data.levelName}' has no sceneName assigned. " +
                           "Fill in the Scene Name field on the LevelData asset and make sure it's added to Build Settings.");
            return;
        }

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


    public void OnLevelCleared()
    {
        if (ActiveLevel == null) { Debug.LogWarning("[RunManager] OnLevelCleared with no ActiveLevel."); return; }

        var cleared = ActiveLevel;
        cleared.ClearState = LevelClearState.Cleared;

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

    public void ClearPendingBuffs()
    {
        PendingBuffChoices.Clear();
    }


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

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public struct LevelPreview
{
    public int  Turns;
    public int  BuffCount;
    public bool HasReCorruptionRisk;
}