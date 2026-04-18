// LevelSelectManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Manages the world map screen.
//
// Setup
// ─────
// In the Inspector, assign ALL 21 level nodes to the allLevelNodes list,
// and all 7 boss nodes to the bossNodes list.
// Each node already has its LevelData pre-assigned on the node itself.
//
// At runtime this manager:
//   1. Loops all 21 nodes — activates the 5 chosen by RunManager, locks the rest
//   2. Loops all 7 boss nodes — all locked until boss is unlocked, then reveals
//      whichever region matches the run's boss
//   3. Handles info panel population and level loading
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    public static LevelSelectManager instance;

    // ── Node References ───────────────────────────────────────────────────────
    [Header("Map Nodes")]
    [Tooltip("Drag all 21 level nodes here (14 regular + 7 boss).")]
    public List<LevelNode> allLevelNodes = new();

    [Tooltip("Drag all 7 boss nodes here separately for easy boss reveal control.")]
    public List<LevelNode> bossNodes = new();

    // ── Info Panel ────────────────────────────────────────────────────────────
    [Header("Info Panel")]
    public GameObject panelRoot;
    public TMP_Text   panelLevelName;
    public TMP_Text   panelRegion;
    public TMP_Text   panelLevelType;
    public TMP_Text   panelCorruption;
    public TMP_Text   panelObjective;
    public TMP_Text   panelTurns;
    public TMP_Text   panelBuffList;
    public TMP_Text   panelReCorruptWarning;
    public Button     btnEnterLevel;
    public Button     btnClose;

    // ── HUD ───────────────────────────────────────────────────────────────────
    [Header("HUD")]
    public TMP_Text hudTurnsText;
    public TMP_Text hudProgressText;

    // ── Boss Unlock UI ────────────────────────────────────────────────────────
    [Header("Boss Unlock")]
    [Tooltip("Optional panel/popup shown when the boss is first revealed.")]
    public GameObject bossUnlockBanner;

    // ─────────────────────────────────────────────────────────────────────────
    private RunLevelState _selectedLevel;

    // ═════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake() => instance = this;

    void Start()
    {
        panelRoot.SetActive(false);
        if (bossUnlockBanner) bossUnlockBanner.SetActive(false);

        // Hide all boss nodes until unlock
        foreach (var node in bossNodes)
            node.LockNode();

        // Wire buttons
        btnEnterLevel?.onClick.AddListener(EnterSelectedLevel);
        btnClose?.onClick.AddListener(ClosePanel);

        // Subscribe to run events
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged  += RefreshAllNodes;
            RunManager.Instance.OnLevelReCorrupted += HandleReCorruption;
            RunManager.Instance.OnBossUnlocked     += HandleBossUnlocked;
        }

        BindNodesToRun();
        RefreshAllNodes();
    }

    void OnDestroy()
    {
        if (RunManager.Instance == null) return;
        RunManager.Instance.OnRunStateChanged  -= RefreshAllNodes;
        RunManager.Instance.OnLevelReCorrupted -= HandleReCorruption;
        RunManager.Instance.OnBossUnlocked     -= HandleBossUnlocked;
    }

    void Update()
    {
        if (panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Node Binding
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Matches each of the 21 nodes against the current run's selected levels.
    /// Nodes whose LevelData is in the run get Activated; the rest get Locked.
    /// </summary>
    private void BindNodesToRun()
    {
        if (RunManager.Instance == null) return;

        // Build a quick lookup: LevelData → RunLevelState
        var runLookup = new Dictionary<LevelData, RunLevelState>();
        foreach (var state in RunManager.Instance.RunLevels)
            runLookup[state.Data] = state;

        // Regular level nodes (non-boss)
        foreach (var node in allLevelNodes)
        {
            if (node.assignedLevelData == null)
            {
                Debug.LogWarning($"[LevelSelectManager] Node '{node.name}' has no assignedLevelData.");
                node.LockNode();
                continue;
            }

            if (runLookup.TryGetValue(node.assignedLevelData, out RunLevelState state))
                node.ActivateNode(state);
            else
                node.LockNode();
        }

        // Boss nodes — all locked at start, revealed by HandleBossUnlocked
        foreach (var node in bossNodes)
            node.LockNode();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Node Refresh
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshAllNodes()
    {
        foreach (var node in allLevelNodes)
            if (node.IsActive) node.RefreshDisplay();

        foreach (var node in bossNodes)
            if (node.IsActive) node.RefreshDisplay();

        RefreshHUD();

        if (panelRoot.activeSelf && _selectedLevel != null)
            PopulatePanel(_selectedLevel);
    }

    private void RefreshHUD()
    {
        if (RunManager.Instance == null) return;

        if (hudTurnsText)
            hudTurnsText.text = $"Turns: {RunManager.Instance.TotalTurns}";

        if (hudProgressText)
        {
            int cleared = 0;
            foreach (var lvl in RunManager.Instance.RunLevels)
                if (lvl.IsCleared) cleared++;
            hudProgressText.text = $"Cleared: {cleared} / {RunManager.Instance.RunLevels.Count}";
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Info Panel
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Called by a LevelNode when the player clicks it.</summary>
    public void SelectLevel(RunLevelState level)
    {
        _selectedLevel = level;

        LevelPreview preview = RunManager.Instance != null
            ? RunManager.Instance.PreviewLevel(level)
            : new LevelPreview { Turns = 1, BuffCount = 1 };

        PopulatePanel(level, preview);
        panelRoot.SetActive(true);
    }

    private void PopulatePanel(RunLevelState level, LevelPreview? preview = null)
    {
        if (panelLevelName)  panelLevelName.text  = level.Data.levelName;
        if (panelRegion)     panelRegion.text     = RegionDistanceHelper.GetDisplayName(level.Data.region);
        if (panelLevelType)  panelLevelType.text  = level.Data.levelType.ToString();
        if (panelCorruption) panelCorruption.text = $"Corruption: {level.CurrentCorruption}";
        if (panelObjective)  panelObjective.text  = $"Objective: {level.Data.missionObjective}";

        // Turns
        if (panelTurns && preview.HasValue)
        {
            int t = preview.Value.Turns;
            panelTurns.text = t == 1
                ? "1 Turn  (Close)"
                : $"{t} Turns  (Further — better rewards)";
        }

        // Buff list
        if (panelBuffList)
        {
            if (level.AssignedBuffs.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>Rewards on Clear:</b>");
                foreach (var buff in level.AssignedBuffs)
                    sb.AppendLine($"  • {buff.GetDescription()}");
                panelBuffList.text = sb.ToString();
            }
            else
            {
                panelBuffList.text = "No rewards assigned.";
            }
        }

        // Re-corruption warning
        bool showWarning = preview.HasValue && preview.Value.HasReCorruptionRisk;
        if (panelReCorruptWarning)
        {
            panelReCorruptWarning.gameObject.SetActive(showWarning);
            if (showWarning)
                panelReCorruptWarning.text = "⚠  Taking this route may re-corrupt nearby cleared levels!";
        }

        if (btnEnterLevel)
            btnEnterLevel.interactable = level.NeedsClearing;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Button Handlers
    // ═════════════════════════════════════════════════════════════════════════

    public void EnterSelectedLevel()
    {
        if (_selectedLevel == null) return;
        ClosePanel();
        RunManager.Instance?.LoadLevel(_selectedLevel);
    }

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
        _selectedLevel = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleReCorruption(RunLevelState level)
    {
        Debug.Log($"[LevelSelectManager] '{level.Data.levelName}' re-corrupted — refreshing map.");
        RefreshAllNodes();
    }

    private void HandleBossUnlocked()
    {
        if (RunManager.Instance?.BossLevel == null) return;

        LevelData bossData = RunManager.Instance.BossLevel.Data;

        // Find the boss node whose pre-assigned LevelData matches the run's boss
        foreach (var node in bossNodes)
        {
            if (node.assignedLevelData == bossData)
            {
                node.SetBossVisible(RunManager.Instance.BossLevel);
                Debug.Log($"[LevelSelectManager] Boss node revealed: {bossData.levelName}");
                break;
            }
        }

        // Show the optional banner
        if (bossUnlockBanner)
        {
            bossUnlockBanner.SetActive(true);
            Invoke(nameof(HideBossBanner), 3f);
        }

        RefreshHUD();
    }

    private void HideBossBanner() 
    { 
        if (bossUnlockBanner) bossUnlockBanner.SetActive(false); 
    }
}