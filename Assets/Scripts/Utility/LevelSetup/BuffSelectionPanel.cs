// BuffSelectionPanel.cs
// ─────────────────────────────────────────────────────────────────────────────
// Shown on the LevelSelect screen after a level is cleared.
// Displays up to 3 buff cards — player clicks one to receive it.
//
// Hierarchy setup:
//
//   BuffSelectionPanel          (this script, hidden by default)
//     Backdrop                  (dark semi-transparent full-screen Image)
//     TitleText                 (TMP_Text — e.g. "Choose Your Reward")
//     Container                 (Horizontal Layout Group)
//       BuffCard_0              (BuffCard script, drag all 3 into buffCards list)
//       BuffCard_1
//       BuffCard_2
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffSelectionPanel : MonoBehaviour
{
    public static BuffSelectionPanel Instance { get; private set; }

    [Header("References")]
    [Tooltip("Drag the 3 BuffCard child objects here.")]
    public List<BuffCard> buffCards = new();

    [Header("Settings")]
    [Tooltip("Hide unused card slots when a level has fewer than 3 buff options.")]
    public bool hideUnusedCards = true;

    /// <summary>Fired once the player picks a buff. LevelSelectManager listens to this.</summary>
    public event Action OnBuffChosen;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Show the panel with the given buff options.
    /// Called by LevelSelectManager when pending buffs exist on scene load.
    /// </summary>
    public void Show(List<BuffReward> options)
    {
        if (options == null || options.Count == 0)
        {
            // No buffs to award — skip the panel entirely
            OnBuffChosen?.Invoke();
            return;
        }

        gameObject.SetActive(true);

        for (int i = 0; i < buffCards.Count; i++)
        {
            if (i < options.Count)
            {
                buffCards[i].gameObject.SetActive(true);
                buffCards[i].Setup(options[i], OnCardClicked);
            }
            else
            {
                if (hideUnusedCards)
                    buffCards[i].gameObject.SetActive(false);
                else
                    buffCards[i].SetEmpty();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnCardClicked(BuffReward chosen)
    {
        PlayerBuffManager.Instance?.AddBuff(chosen);
        Debug.Log($"[BuffSelection] Player chose: {chosen.GetDescription()}");

        gameObject.SetActive(false);
        OnBuffChosen?.Invoke();
    }
}