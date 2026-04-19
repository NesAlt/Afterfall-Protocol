using System.Collections.Generic;

public enum LevelClearState
{
    Uncleared,    // Not yet attempted this run
    Cleared,      // Successfully completed
    ReCorrupted   // Was cleared but the re-corruption mechanic reset it
}

public class RunLevelState
{
    public LevelData Data { get; }
    public int CurrentCorruption { get; set; }

    public LevelClearState ClearState { get; set; } = LevelClearState.Uncleared;

    public List<BuffReward> AssignedBuffs { get; } = new();

    public int TimesReCorrupted { get; set; }

    public bool IsCleared      => ClearState == LevelClearState.Cleared;
    public bool NeedsClearing  => ClearState != LevelClearState.Cleared;

    public RunLevelState(LevelData data, int startingCorruption = 10)
    {
        Data               = data;
        CurrentCorruption  = startingCorruption;
    }
}