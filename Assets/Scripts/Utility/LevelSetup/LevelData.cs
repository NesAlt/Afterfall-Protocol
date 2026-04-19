using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public string levelName;

    [Tooltip("The exact Unity scene name to load for this level.")]
    public string sceneName;

    public WorldRegion region;
    public LevelType   levelType;

    [Header("Narrative")]
    [TextArea(2, 4)]
    public string missionObjective;

    [Header("Buff Pool")]
    [Tooltip(
        "All possible buffs this level can award. At run time RunManager draws " +
        "1–3 of these depending on geographic distance from the player's last position. " +
        "Add more entries to this pool for higher-value levels — the pool is shuffled " +
        "and the first N entries are used.")]
    public List<BuffReward> buffPool = new();
}