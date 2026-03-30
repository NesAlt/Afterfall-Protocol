using UnityEngine;
public class SampleManager : MonoBehaviour
{
    public static SampleManager Instance;

    public int currentSamples = 0;
    public int requiredSamples = 10;

    [Header("UI")]
    [SerializeField] private SampleUI sampleUI;

    [Header("Victory")]
    [SerializeField] private GameObject victoryPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddSample(int amount)
    {
        currentSamples += amount;

        UpdateUI();

        if (currentSamples >= requiredSamples)
        {
            CompleteLevel();
        }
    }

    void UpdateUI()
    {
        if (sampleUI != null)
        {
            sampleUI.UpdateUI(currentSamples, requiredSamples);
        }
    }

    void CompleteLevel()
    {
        Debug.Log("LEVEL COMPLETE");

         ArenaController arena = FindObjectOfType<ArenaController>();
        if (arena != null)
        {
            arena.ForceEndArena();
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        Time.timeScale = 0f; // pause game
    }
}