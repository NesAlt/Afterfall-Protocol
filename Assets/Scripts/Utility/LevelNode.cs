// LevelNode.cs
// ─────────────────────────────────────────────────────────────────────────────
// Attach to every level icon on your world map (21 total).
// Each node is pre-assigned a LevelData asset in the inspector at edit time.
//
// At runtime LevelSelectManager calls one of three methods:
//   ActivateNode(state)  — this level was chosen this run, make it playable
//   LockNode()           — not chosen this run, show as greyed/locked
//   SetBossVisible(state)— for boss nodes only, revealed after 5 clears
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    // ── Inspector — set at edit time ──────────────────────────────────────────
    [Header("Pre-Assigned Level (set in Inspector)")]
    public LevelData assignedLevelData;

    [Header("UI References")]
    public TMP_Text levelNameText;
    public TMP_Text regionText;
    public TMP_Text corruptionText;
    public TMP_Text statusText;
    public Image    nodeIcon;
    public Button   selectButton;

    [Header("Active State Colours")]
    public Color unclearedColour   = Color.white;
    public Color clearedColour     = new Color(0.4f, 1f, 0.4f);
    public Color reCorruptedColour = new Color(1f, 0.3f, 0.3f);
    public Color bossColour        = new Color(1f, 0.6f, 0f);

    [Header("Inactive State")]
    public Color lockedColour      = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [Tooltip("Optional separate object to show a lock icon over the node.")]
    public GameObject lockOverlay;

    // ── Runtime State ─────────────────────────────────────────────────────────
    public RunLevelState LevelState  { get; private set; }
    public bool          IsActive    { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    // Called by LevelSelectManager
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>This node's level was selected for the current run — make it interactive.</summary>
    public void ActivateNode(RunLevelState state)
    {
        LevelState = state;
        IsActive   = true;

        if (lockOverlay) lockOverlay.SetActive(false);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnNodeClicked);
        selectButton.interactable = true;

        RefreshDisplay();
    }

    /// <summary>This node's level was not selected this run — grey it out.</summary>
    public void LockNode()
    {
        LevelState = null;
        IsActive   = false;

        if (lockOverlay) lockOverlay.SetActive(true);

        selectButton.onClick.RemoveAllListeners();
        selectButton.interactable = false;

        // Show the node's static data in muted colours so the map still looks full
        if (assignedLevelData != null)
        {
            if (levelNameText)  levelNameText.text  = assignedLevelData.levelName;
            if (regionText)     regionText.text     = RegionDistanceHelper.GetDisplayName(assignedLevelData.region);
            if (corruptionText) corruptionText.text = "";
            if (statusText)     statusText.text     = "Not Active";
        }

        if (nodeIcon) nodeIcon.color = lockedColour;
    }

    /// <summary>
    /// For boss nodes — hidden until the run's boss is revealed.
    /// Pass the boss RunLevelState when showing, null when hiding.
    /// </summary>
    public void SetBossVisible(RunLevelState bossState)
    {
        if (bossState != null)
            ActivateNode(bossState);
        else
            LockNode();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Display Refresh
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Re-reads LevelState and updates all UI elements.</summary>
    public void RefreshDisplay()
    {
        if (!IsActive || LevelState == null) return;

        if (levelNameText)  levelNameText.text  = LevelState.Data.levelName;
        if (regionText)     regionText.text     = RegionDistanceHelper.GetDisplayName(LevelState.Data.region);
        if (corruptionText) corruptionText.text = $"Corruption: {LevelState.CurrentCorruption}";

        if (statusText)
            statusText.text = LevelState.ClearState switch
            {
                LevelClearState.Cleared     => "✓ Cleared",
                LevelClearState.ReCorrupted => "⚠ Re-Corrupted!",
                _ => LevelState.Data.levelType == LevelType.Boss ? "BOSS" : "Uncleared"
            };

        if (nodeIcon)
            nodeIcon.color = LevelState.ClearState switch
            {
                LevelClearState.Cleared     => clearedColour,
                LevelClearState.ReCorrupted => reCorruptedColour,
                _ => LevelState.Data.levelType == LevelType.Boss ? bossColour : unclearedColour
            };

        // Cleared nodes (that haven't been re-corrupted) are not clickable
        if (selectButton)
            selectButton.interactable = LevelState.NeedsClearing;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnNodeClicked()
    {
        LevelSelectManager.instance?.SelectLevel(LevelState);
    }
}