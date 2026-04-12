// RunLevelState.cs
// Runtime wrapper around a LevelData asset that tracks mutable per-run state:
// corruption, clear status, assigned buff rewards, and how many times the
// level has been re-corrupted.
//
// RunManager holds a List<RunLevelState> for the current run's 5 levels.
// These objects live entirely in memory — no ScriptableObject mutation occurs.

using System.Collections.Generic;

public enum LevelClearState
{
    Uncleared,    // Not yet attempted this run
    Cleared,      // Successfully completed
    ReCorrupted   // Was cleared but the re-corruption mechanic reset it
}

public class RunLevelState
{
    // ── Static Data (from ScriptableObject) ──────────────────────────────────
    public LevelData Data { get; }

    // ── Dynamic State ────────────────────────────────────────────────────────

    /// <summary>Current corruption value. Starts at 10, increases each time other levels are cleared.</summary>
    public int CurrentCorruption { get; set; }

    public LevelClearState ClearState { get; set; } = LevelClearState.Uncleared;

    /// <summary>The specific buffs drawn from Data.buffPool for this run instance.</summary>
    public List<BuffReward> AssignedBuffs { get; } = new();

    /// <summary>Counts how many times re-corruption has reset this level — used to escalate buff rewards.</summary>
    public int TimesReCorrupted { get; set; }

    // ── Computed Helpers ─────────────────────────────────────────────────────
    public bool IsCleared      => ClearState == LevelClearState.Cleared;
    public bool NeedsClearing  => ClearState != LevelClearState.Cleared;

    // ── Constructor ──────────────────────────────────────────────────────────
    public RunLevelState(LevelData data, int startingCorruption = 10)
    {
        Data               = data;
        CurrentCorruption  = startingCorruption;
    }
}