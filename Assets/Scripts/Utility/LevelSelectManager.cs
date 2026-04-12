// LevelSelectManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Orchestrates the world-map level-select screen.
//
// Responsibilities
// ────────────────
//  • Binds 5 LevelNodes to the current run's RunLevelStates
//  • Shows / hides the boss node when all levels are cleared
//  • Populates the info panel (name, region, corruption, objective,
//    turn cost, buff rewards, re-corruption warning)
//  • Forwards Enter/Close button actions to RunManager
//  • Refreshes all nodes whenever RunManager fires OnRunStateChanged
//
// Inspector Setup
// ───────────────
//  levelNodes        — 5 pre-placed LevelNode objects on your world map
//  bossNode          — 1 LevelNode for the boss, hidden until boss is unlocked
//  All TMP_Text and Button references in the info panel section
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
    [Tooltip("Assign 5 LevelNode GameObjects placed on your world map.")]
    public List<LevelNode> levelNodes = new();

    [Tooltip("The boss node — hidden until all 5 levels are cleared.")]
    public LevelNode bossNode;

    // ── Info Panel ────────────────────────────────────────────────────────────
    [Header("Info Panel")]
    public GameObject infoPanel;
    public TMP_Text   panelLevelName;
    public TMP_Text   panelRegion;
    public TMP_Text   panelLevelType;
    public TMP_Text   panelCorruption;
    public TMP_Text   panelObjective;
    public TMP_Text   panelTurns;
    public TMP_Text   panelBuffList;
    public TMP_Text   panelReCorruptWarning;
    public Button     btnEnterLevel;
    public Button     btnEnterBoss;
    public Button     btnClose;

    // ── HUD ───────────────────────────────────────────────────────────────────
    [Header("HUD")]
    public TMP_Text hudTurnsText;
    public TMP_Text hudProgressText;

    // ─────────────────────────────────────────────────────────────────────────
    private RunLevelState _selectedLevel;

    // ═════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake() => instance = this;

    void Start()
    {
        // Hide panels
        infoPanel.SetActive(false);
        if (bossNode) bossNode.gameObject.SetActive(false);
        if (btnEnterBoss) btnEnterBoss.gameObject.SetActive(false);

        // Wire buttons
        btnEnterLevel?.onClick.AddListener(EnterSelectedLevel);
        btnEnterBoss?.onClick.AddListener(EnterBossLevel);
        btnClose?.onClick.AddListener(ClosePanel);

        // Subscribe to run events
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged  += RefreshAllNodes;
            RunManager.Instance.OnLevelReCorrupted += HandleReCorruption;
            RunManager.Instance.OnBossUnlocked     += HandleBossUnlocked;
        }

        InitialiseNodes();
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
        if (infoPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Initialisation
    // ═════════════════════════════════════════════════════════════════════════

    private void InitialiseNodes()
    {
        if (RunManager.Instance == null) return;

        var runLevels = RunManager.Instance.RunLevels;
        for (int i = 0; i < levelNodes.Count; i++)
        {
            if (i < runLevels.Count)
            {
                levelNodes[i].gameObject.SetActive(true);
                levelNodes[i].Initialize(runLevels[i]);
            }
            else
            {
                levelNodes[i].gameObject.SetActive(false);
            }
        }

        if (bossNode != null && RunManager.Instance.BossLevel != null)
            bossNode.Initialize(RunManager.Instance.BossLevel);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Node Refresh
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshAllNodes()
    {
        foreach (var node in levelNodes)
            if (node.gameObject.activeSelf) node.RefreshDisplay();

        bossNode?.RefreshDisplay();
        RefreshHUD();

        // If the info panel is open, refresh it too in case state changed
        if (infoPanel.activeSelf && _selectedLevel != null)
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
    // Panel Population
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Called by a LevelNode when the player clicks it.</summary>
    public void SelectLevel(RunLevelState level)
    {
        _selectedLevel = level;

        // Ask RunManager to re-assign buffs based on current distance, get preview data
        LevelPreview preview = RunManager.Instance != null
            ? RunManager.Instance.PreviewLevel(level)
            : new LevelPreview { Turns = 1, BuffCount = 1 };

        PopulatePanel(level, preview);
        infoPanel.SetActive(true);
    }

    private void PopulatePanel(RunLevelState level, LevelPreview? preview = null)
    {
        // ── Identity ─────────────────────────────────────────────────────────
        if (panelLevelName)  panelLevelName.text  = level.Data.levelName;
        if (panelRegion)     panelRegion.text      = RegionDistanceHelper.GetDisplayName(level.Data.region);
        if (panelLevelType)  panelLevelType.text   = level.Data.levelType.ToString();
        if (panelCorruption) panelCorruption.text  = $"Corruption: {level.CurrentCorruption}";
        if (panelObjective)  panelObjective.text   = $"Objective: {level.Data.missionObjective}";

        // ── Turns & Distance ─────────────────────────────────────────────────
        if (panelTurns && preview.HasValue)
        {
            int turns = preview.Value.Turns;
            panelTurns.text = turns == 1
                ? "1 Turn  (Close)"
                : $"{turns} Turns  (Further — better rewards)";
        }

        // ── Buff Rewards ─────────────────────────────────────────────────────
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
                panelBuffList.text = "No reward data yet.";
            }
        }

        // ── Re-Corruption Warning ─────────────────────────────────────────────
        bool showWarning = preview.HasValue && preview.Value.HasReCorruptionRisk;
        if (panelReCorruptWarning)
        {
            panelReCorruptWarning.gameObject.SetActive(showWarning);
            if (showWarning)
                panelReCorruptWarning.text = "⚠  Taking this route may re-corrupt nearby cleared levels!";
        }

        // ── Buttons ───────────────────────────────────────────────────────────
        if (btnEnterLevel)
            btnEnterLevel.interactable = level.NeedsClearing;

        bool bossReady = RunManager.Instance?.BossUnlocked ?? false;
        if (btnEnterBoss)
            btnEnterBoss.gameObject.SetActive(bossReady);
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

    public void EnterBossLevel()
    {
        ClosePanel();
        RunManager.Instance?.LoadBoss();
    }

    public void ClosePanel()
    {
        infoPanel.SetActive(false);
        _selectedLevel = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleReCorruption(RunLevelState level)
    {
        Debug.Log($"[LevelSelectManager] '{level.Data.levelName}' has been re-corrupted.");
        // TODO: Play a VFX / flash the node, show a popup, etc.
        RefreshAllNodes();
    }

    private void HandleBossUnlocked()
    {
        if (bossNode != null)
        {
            bossNode.gameObject.SetActive(true);
            bossNode.RefreshDisplay();
        }
        if (btnEnterBoss) btnEnterBoss.gameObject.SetActive(true);

        Debug.Log("[LevelSelectManager] Boss node revealed!");
        // TODO: Play boss-unlock fanfare
    }
}