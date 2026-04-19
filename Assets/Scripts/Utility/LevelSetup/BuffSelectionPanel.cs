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

    public event Action OnBuffChosen;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

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

    private void OnCardClicked(BuffReward chosen)
    {
        PlayerBuffManager.Instance?.AddBuff(chosen);
        Debug.Log($"[BuffSelection] Player chose: {chosen.GetDescription()}");

        gameObject.SetActive(false);
        OnBuffChosen?.Invoke();
    }
}