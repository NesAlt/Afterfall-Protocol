// LevelEndPickup.cs
using UnityEngine;

public class LevelEndItem : MonoBehaviour
{
    public VictoryUIController victoryUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        LevelManager.Instance?.NotifyLevelCleared();

        // 2. Show victory UI
        victoryUI?.ShowVictory();

        Destroy(gameObject);
    }
}