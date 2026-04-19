using UnityEngine;

public class ArenaController : MonoBehaviour
{
    [Header("Arena Settings")]
    public int requiredKills = 5;

    [Header("References")]
    public EnemySpawner spawner;
    public Transform    player;
    public DoorController[] doors;

    private int  currentKills      = 0;
    private bool arenaActive       = false;
    private bool finalEnemySpawned = false;
    private bool arenaCleared      = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!arenaActive && other.CompareTag("Player"))
            StartArena();
    }

    void StartArena()
    {
        arenaActive = true;

        foreach (DoorController door in doors)
            door.CloseDoor();

        bool isBoss = LevelManager.Instance != null && LevelManager.Instance.IsBossLevel();

        if (isBoss)
            spawner.SpawnBoss(this);
        else
            spawner.StartSpawning(this);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void RegisterKill(bool isFinalEnemy)
    {
        if (arenaCleared) return;

        // KillAndCollect levels track completion via SampleManager
        if (LevelManager.Instance != null && LevelManager.Instance.IsKillAndCollectLevel())
            return;

        // Boss level — boss death comes through here as a final enemy
        if (LevelManager.Instance != null && LevelManager.Instance.IsBossLevel())
        {
            arenaCleared = true;
            OpenDoors();
            spawner.StopSpawning();
            LevelManager.Instance.NotifyLevelCleared(); // → RunManager.OnBossCleared()
            VictoryUIController.Instance?.ShowVictory();
            return;
        }

        // AreaClear level
        if (isFinalEnemy)
        {
            arenaCleared = true;
            EndArena();
            return;
        }

        currentKills++;

        if (currentKills >= requiredKills && !finalEnemySpawned)
        {
            finalEnemySpawned = true;
            spawner.StopSpawning();
            spawner.SpawnFinalEnemy();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called when the final enemy in an AreaClear level dies
    void EndArena()
    {
        OpenDoors();
        spawner.StopSpawning();
        // LevelEndPickup handles NotifyLevelCleared + ShowVictory for AreaClear
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called by SampleManager when sample quota is reached (KillAndCollect)
    public void ForceEndArena()
    {
        if (arenaCleared) return;
        arenaCleared = true;
        OpenDoors();
        spawner.StopSpawning();
        // SampleManager already called NotifyLevelCleared + ShowVictory before this
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OpenDoors()
    {
        foreach (DoorController door in doors)
            door.OpenDoor();
    }
}