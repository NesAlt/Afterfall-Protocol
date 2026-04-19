// LevelSelectManager.cs

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
    public List<LevelNode> allLevelNodes = new();
    public List<LevelNode> bossNodes     = new();

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

    // ── Buff Selection ────────────────────────────────────────────────────────
    [Header("Buff Selection")]
    [Tooltip("The BuffSelectionPanel GameObject — hidden by default.")]
    public BuffSelectionPanel buffSelectionPanel;

    // ── Re-Corruption Overlay ─────────────────────────────────────────────────
    [Header("Re-Corruption Overlay")]
    [Tooltip("Full-width panel anchored to top-center. Hidden by default.")]
    public GameObject reCorruptOverlay;
    [Tooltip("Text inside the overlay — shows which level was re-corrupted.")]
    public TMP_Text   reCorruptOverlayText;
    [Tooltip("How long the overlay stays visible before auto-hiding.")]
    public float      reCorruptOverlayDuration = 4f;

    // ── Boss Unlock ───────────────────────────────────────────────────────────
    [Header("Boss Unlock")]
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
        if (reCorruptOverlay)  reCorruptOverlay.SetActive(false);
        if (bossUnlockBanner)  bossUnlockBanner.SetActive(false);

        foreach (var node in bossNodes) node.gameObject.SetActive(false);

        btnEnterLevel?.onClick.AddListener(EnterSelectedLevel);
        btnClose?.onClick.AddListener(ClosePanel);

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged  += RefreshAllNodes;
            RunManager.Instance.OnLevelReCorrupted += HandleReCorruption;
            RunManager.Instance.OnBossUnlocked     += HandleBossUnlocked;
        }

        if (buffSelectionPanel != null)
            buffSelectionPanel.OnBuffChosen += OnBuffChosen;

        BindNodesToRun();
        RefreshAllNodes();

        // ── Show buff selection if the player just cleared a level ────────────
        // PendingBuffChoices is populated in RunManager.OnLevelCleared() and
        // persists across the scene load until the player makes their choice.
        if (RunManager.Instance?.PendingBuffChoices?.Count > 0)
            buffSelectionPanel?.Show(RunManager.Instance.PendingBuffChoices);
    }

    void OnDestroy()
    {
        if (RunManager.Instance == null) return;
        RunManager.Instance.OnRunStateChanged  -= RefreshAllNodes;
        RunManager.Instance.OnLevelReCorrupted -= HandleReCorruption;
        RunManager.Instance.OnBossUnlocked     -= HandleBossUnlocked;

        if (buffSelectionPanel != null)
            buffSelectionPanel.OnBuffChosen -= OnBuffChosen;
    }

    void Update()
    {
        if (panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Node Binding
    // ═════════════════════════════════════════════════════════════════════════

    private void BindNodesToRun()
    {
        if (RunManager.Instance == null) return;

        var runLookup = new Dictionary<LevelData, RunLevelState>();
        foreach (var state in RunManager.Instance.RunLevels)
            runLookup[state.Data] = state;

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

        foreach (var node in bossNodes) node.gameObject.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Node Refresh
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshAllNodes()
    {
        foreach (var node in allLevelNodes) if (node.IsActive) node.RefreshDisplay();
        foreach (var node in bossNodes)     if (node.IsActive) node.RefreshDisplay();
        RefreshHUD();
        if (panelRoot.activeSelf && _selectedLevel != null) PopulatePanel(_selectedLevel);
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

        if (panelTurns && preview.HasValue)
        {
            int t = preview.Value.Turns;
            panelTurns.text = t == 1 ? "1 Turn  (Close)" : $"{t} Turns  (Further — better rewards)";
        }

        // Show preview of possible buffs — player sees these before entering
        if (panelBuffList)
        {
            if (level.AssignedBuffs.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>Possible Rewards:</b>");
                foreach (var buff in level.AssignedBuffs)
                    sb.AppendLine($"  • {buff.GetDescription()}");
                panelBuffList.text = sb.ToString();
            }
            else
            {
                panelBuffList.text = "No rewards assigned.";
            }
        }

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
    // Buff Selection
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Called by BuffSelectionPanel.OnBuffChosen once the player picks.</summary>
    private void OnBuffChosen()
    {
        RunManager.Instance?.ClearPendingBuffs();

        // If boss was just cleared, go to run-complete flow instead of refreshing map
        if (RunManager.Instance?.BossLevel?.IsCleared == true)
        {
            // TODO: load credits / run summary scene here
            Debug.Log("[LevelSelectManager] Run complete — handle end screen here.");
            return;
        }

        RefreshAllNodes();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Re-Corruption Overlay
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleReCorruption(RunLevelState level)
    {
        RefreshAllNodes();

        if (reCorruptOverlay == null) return;

        if (reCorruptOverlayText)
            reCorruptOverlayText.text = $"⚠  {level.Data.levelName} has been re-corrupted!";

        reCorruptOverlay.SetActive(true);
        CancelInvoke(nameof(HideReCorruptOverlay));
        Invoke(nameof(HideReCorruptOverlay), reCorruptOverlayDuration);
    }

    private void HideReCorruptOverlay()
    {
        if (reCorruptOverlay) reCorruptOverlay.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Boss Unlock
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleBossUnlocked()
    {
        if (RunManager.Instance?.BossLevel == null) return;

        LevelData bossData = RunManager.Instance.BossLevel.Data;

        foreach (var node in bossNodes)
        {
            if (node.assignedLevelData == bossData)
            {
                node.SetBossVisible(RunManager.Instance.BossLevel);
                Debug.Log($"[LevelSelectManager] Boss node revealed: {bossData.levelName}");
                break;
            }
        }

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