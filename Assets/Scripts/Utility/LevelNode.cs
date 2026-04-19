using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{

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

    public RunLevelState LevelState  { get; private set; }
    public bool          IsActive    { get; private set; }

    public void ActivateNode(RunLevelState state)
    {
        LevelState = state;
        IsActive   = true;

        if (lockOverlay) lockOverlay.SetActive(false);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnNodeClicked);
            selectButton.interactable = true;
        }

        RefreshDisplay();
    }

    public void LockNode()
    {
        LevelState = null;
        IsActive   = false;
        gameObject.SetActive(false);
    }

    public void SetBossVisible(RunLevelState bossState)
    {
        if (bossState != null)
            ActivateNode(bossState);
        else
            LockNode();
    }

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

    private void OnNodeClicked()
    {
        LevelSelectManager.instance?.SelectLevel(LevelState);
    }
}