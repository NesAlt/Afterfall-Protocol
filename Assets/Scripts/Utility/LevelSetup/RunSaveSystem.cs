using System;
using System.Collections.Generic;
using UnityEngine;

public static class RunSaveSystem
{
    private const string SaveKey = "RunSaveData";

    public static bool HasSave() => PlayerPrefs.HasKey(SaveKey);

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[RunSaveSystem] Save deleted.");
    }


    public static void SaveRun(RunManager runManager)
    {
        var data = new SaveData();

        // Run levels
        foreach (var lvl in runManager.RunLevels)
            data.levels.Add(SerialiseLevel(lvl));

        // Boss
        if (runManager.BossLevel != null)
            data.boss = SerialiseLevel(runManager.BossLevel);

        // Run meta
        data.totalTurns = runManager.TotalTurns;

        var levelList = runManager.RunLevels as IList<RunLevelState>;
        int lastIdx = levelList != null ? levelList.IndexOf(runManager.LastClearedLevel) : -1;
        data.lastClearedIndex = lastIdx; // -1 if null (nothing cleared yet)

        // Player buffs
        if (PlayerBuffManager.Instance != null)
            foreach (var buff in PlayerBuffManager.Instance.GetAllBuffs())
                data.playerBuffs.Add(new SerialisedBuff
                {
                    buffType = (int)buff.buffType,
                    value    = buff.value
                });

        string json = JsonUtility.ToJson(data, prettyPrint: false);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log("[RunSaveSystem] Run saved.");
    }

    public static bool LoadRun(RunManager runManager)
    {
        if (!HasSave())
        {
            Debug.LogWarning("[RunSaveSystem] No save found.");
            return false;
        }

        string json = PlayerPrefs.GetString(SaveKey);

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RunSaveSystem] Failed to parse save data: {e.Message}");
            DeleteSave();
            return false;
        }

        var rebuiltLevels = new List<RunLevelState>();
        foreach (var saved in data.levels)
        {
            var state = DeserialiseLevel(saved, runManager);
            if (state == null)
            {
                Debug.LogError("[RunSaveSystem] Could not resolve a saved level — save may be corrupt. Starting fresh.");
                DeleteSave();
                return false;
            }
            rebuiltLevels.Add(state);
        }

        RunLevelState bossState = null;
        if (data.boss != null)
        {
            bossState = DeserialiseLevel(data.boss, runManager);
            if (bossState == null)
            {
                Debug.LogError("[RunSaveSystem] Could not resolve saved boss level.");
                DeleteSave();
                return false;
            }
        }

        runManager.LoadFromSave(
            rebuiltLevels,
            bossState,
            data.totalTurns,
            data.lastClearedIndex
        );

        PlayerBuffManager.Instance?.ResetBuffs();
        if (PlayerBuffManager.Instance != null)
        {
            foreach (var sb in data.playerBuffs)
            {
                PlayerBuffManager.Instance.AddBuff(new BuffReward
                {
                    buffType = (BuffType)sb.buffType,
                    value    = sb.value
                });
            }
        }

        Debug.Log($"[RunSaveSystem] Run loaded — {rebuiltLevels.Count} levels, turn {data.totalTurns}.");
        return true;
    }

    private static SerialisedLevel SerialiseLevel(RunLevelState lvl)
    {
        var s = new SerialisedLevel
        {
            regionIndex    = (int)lvl.Data.region,
            levelTypeIndex = (int)lvl.Data.levelType,
            clearState     = (int)lvl.ClearState,
            corruption     = lvl.CurrentCorruption,
            timesReCorrupted = lvl.TimesReCorrupted
        };

        foreach (var buff in lvl.AssignedBuffs)
            s.assignedBuffs.Add(new SerialisedBuff
            {
                buffType = (int)buff.buffType,
                value    = buff.value
            });

        return s;
    }

    private static RunLevelState DeserialiseLevel(SerialisedLevel s, RunManager rm)
    {
        // Re-look up the LevelData asset using region + type indices
        LevelData data = GetLevelData(rm, (WorldRegion)s.regionIndex, (LevelType)s.levelTypeIndex);
        if (data == null) return null;

        var state = new RunLevelState(data, s.corruption)
        {
            ClearState       = (LevelClearState)s.clearState,
            TimesReCorrupted = s.timesReCorrupted
        };

        foreach (var sb in s.assignedBuffs)
            state.AssignedBuffs.Add(new BuffReward
            {
                buffType = (BuffType)sb.buffType,
                value    = sb.value
            });

        return state;
    }

    private static LevelData GetLevelData(RunManager rm, WorldRegion region, LevelType type)
    {
        int idx = (int)region;
        return type switch
        {
            LevelType.AreaClear      => SafeGet(rm.areaClearLevels,   idx),
            LevelType.KillAndCollect => SafeGet(rm.killCollectLevels, idx),
            LevelType.Boss           => SafeGet(rm.bossLevels,        idx),
            _                        => null
        };
    }

    private static LevelData SafeGet(LevelData[] array, int index)
    {
        if (array == null || index < 0 || index >= array.Length) return null;
        return array[index];
    }


    [Serializable]
    private class SaveData
    {
        public List<SerialisedLevel> levels      = new();
        public SerialisedLevel       boss         = null;
        public int                   totalTurns   = 0;
        public int                   lastClearedIndex = -1;
        public List<SerialisedBuff>  playerBuffs  = new();
    }

    [Serializable]
    private class SerialisedLevel
    {
        public int regionIndex;
        public int levelTypeIndex;
        public int clearState;
        public int corruption;
        public int timesReCorrupted;
        public List<SerialisedBuff> assignedBuffs = new();
    }

    [Serializable]
    private class SerialisedBuff
    {
        public int   buffType;
        public float value;
    }
}