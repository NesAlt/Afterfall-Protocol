// SampleManager.cs
using UnityEngine;

public class SampleManager : MonoBehaviour
{
    public static SampleManager Instance;

    public int currentSamples  = 0;
    public int requiredSamples = 10;

    [Header("UI")]
    [SerializeField] private SampleUI sampleUI;

    [Header("Victory")]
    [SerializeField] private GameObject victoryPanel;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void AddSample(int amount)
    {
        currentSamples += amount;
        UpdateUI();

        if (currentSamples >= requiredSamples)
            CompleteLevel();
    }

    void UpdateUI()
    {
        sampleUI?.UpdateUI(currentSamples, requiredSamples);
    }

    void CompleteLevel()
    {
        Debug.Log("[SampleManager] Sample quota reached — level complete.");

        // 1. Show victory panel
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (VictoryUIController.Instance != null)
            VictoryUIController.Instance.ShowVictory();

        // 2. Tell the arena to open doors / stop spawning
        //    ArenaController.ForceEndArena() will then call
        //    LevelManager.NotifyLevelCleared() to update the run system
        ArenaController arena = FindObjectOfType<ArenaController>();
        if (arena != null)
            arena.ForceEndArena();

        Time.timeScale = 0f;
    }
}