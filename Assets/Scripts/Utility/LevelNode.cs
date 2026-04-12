// LevelNode.cs
// ─────────────────────────────────────────────────────────────────────────────
// UI component attached to each level button/icon on the world map.
// Initialised by LevelSelectManager with a RunLevelState, then calls
// RefreshDisplay() whenever run state changes.
//
// Wire the 7 regional map positions in the scene. LevelSelectManager will
// activate/deactivate nodes and call Initialize() with the correct state.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNameText;
    public TMP_Text regionText;
    public TMP_Text corruptionText;
    public TMP_Text statusText;
    public Image    nodeIcon;         // Background/icon image that changes colour by status
    public Button   selectButton;

    [Header("Status Colours")]
    public Color unclearedColour   = Color.white;
    public Color clearedColour     = new Color(0.4f, 1f, 0.4f);   // soft green
    public Color reCorruptedColour = new Color(1f, 0.3f, 0.3f);   // red
    public Color bossColour        = new Color(1f, 0.6f, 0f);     // orange

    // ── State ─────────────────────────────────────────────────────────────────
    public RunLevelState LevelState { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Call once to bind a RunLevelState to this node.</summary>
    public void Initialize(RunLevelState state)
    {
        LevelState = state;
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnNodeClicked);
        RefreshDisplay();
    }

    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Refreshes all visual elements to match the current LevelState.</summary>
    public void RefreshDisplay()
    {
        if (LevelState == null) return;

        // ── Text ─────────────────────────────────────────────────────────────
        if (levelNameText) levelNameText.text = LevelState.Data.levelName;
        if (regionText)    regionText.text    = RegionDistanceHelper.GetDisplayName(LevelState.Data.region);

        if (corruptionText)
            corruptionText.text = $"Corruption: {LevelState.CurrentCorruption}";

        if (statusText)
            statusText.text = LevelState.ClearState switch
            {
                LevelClearState.Cleared      => "✓ Cleared",
                LevelClearState.ReCorrupted  => "⚠ Re-Corrupted!",
                _                            => LevelState.Data.levelType == LevelType.Boss
                                                    ? "BOSS" : "Uncleared"
            };

        // ── Colour ───────────────────────────────────────────────────────────
        if (nodeIcon)
        {
            nodeIcon.color = LevelState.ClearState switch
            {
                LevelClearState.Cleared     => clearedColour,
                LevelClearState.ReCorrupted => reCorruptedColour,
                _ => LevelState.Data.levelType == LevelType.Boss
                         ? bossColour : unclearedColour
            };
        }

        // ── Interactability ──────────────────────────────────────────────────
        // Cleared (and not re-corrupted) nodes are not selectable
        if (selectButton)
            selectButton.interactable = LevelState.NeedsClearing;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnNodeClicked()
    {
        LevelSelectManager.instance?.SelectLevel(LevelState);
    }
}