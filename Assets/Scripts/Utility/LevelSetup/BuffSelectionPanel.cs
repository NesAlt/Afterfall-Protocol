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
        Debug.Log($"[BuffSelectionPanel] Awake ID: {GetInstanceID()}");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BuffSelectionPanel destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf)
            Debug.Log("Panel got disabled externally!");
    }

    public void Show(List<BuffReward> options)
    {
        // Debug.Log($"[BuffSelectionPanel] Show called on ID: {GetInstanceID()}");
        // Debug.Log("[BuffSelectionPanel] SHOW CALLED");
        gameObject.SetActive(true);
        Debug.Log($"[BuffSelectionPanel] Active after SetActive: {gameObject.activeSelf}");

        Debug.Log("Panel activated");
        Debug.Log($"[BuffSelectionPanel] Instance ID: {GetInstanceID()}");
        if (options == null || options.Count == 0)
        {

            OnBuffChosen?.Invoke();
            return;
        }

        Debug.Log($"[BuffSelectionPanel] Show called with {options?.Count} options");
        gameObject.SetActive(true);

        for (int i = 0; i < buffCards.Count; i++)
        {
            if (i < options.Count)
            {
                Debug.Log($"Setting card {i} with {options[i].buffType}");
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
    void OnDisable()
    {
        Debug.Log("[BuffSelectionPanel] DISABLED");
    }

    private void OnCardClicked(BuffReward chosen)
    {
        PlayerBuffManager.Instance?.AddBuff(chosen);
        Debug.Log($"[BuffSelection] Player chose: {chosen.GetDescription()}");

        gameObject.SetActive(false);
        OnBuffChosen?.Invoke();
    }
}