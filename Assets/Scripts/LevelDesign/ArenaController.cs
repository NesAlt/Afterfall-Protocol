using UnityEngine;

public class ArenaController : MonoBehaviour
{
    [Header("Arena Settings")]
    public int requiredKills = 5;

    [Header("References")]
    public EnemySpawner spawner;
    public Transform player;
    public DoorController[] doors;

    private int  currentKills       = 0;
    private bool arenaActive        = false;
    private bool finalEnemySpawned  = false;
    private bool arenaCleared       = false;

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

        // Guard: LevelManager may be null when testing a scene that has no
        // LevelManager in it; default to normal spawning in that case.
        bool isBoss = LevelManager.Instance != null && LevelManager.Instance.IsBossLevel();

        if (isBoss)
            spawner.SpawnBoss(this);
        else
            spawner.StartSpawning(this);
    }

    public void RegisterKill(bool isFinalEnemy)
    {
        if (arenaCleared) return;

        // KillAndCollect levels track kills via SampleManager, not here
        if (LevelManager.Instance != null && LevelManager.Instance.IsKillAndCollectLevel())
            return;

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

    void EndArena()
    {
        foreach (DoorController door in doors)
            door.OpenDoor();

        spawner.StopSpawning();
    }

    public void ForceEndArena()
    {
        if (arenaCleared) return;
        arenaCleared = true;

        foreach (DoorController door in doors)
            door.OpenDoor();

        spawner.StopSpawning();

        bool isBoss = LevelManager.Instance != null && LevelManager.Instance.IsBossLevel();

        if (isBoss)
        {
            if (VictoryUIController.Instance != null)
                VictoryUIController.Instance.ShowVictory();

            // Notify RunManager that the boss is done
            LevelManager.Instance?.NotifyLevelCleared();
        }
    }
}