using UnityEngine;

public class ArenaController : MonoBehaviour
{
    [Header("Arena Settings")]
    public int requiredKills = 5;

    [Header("References")]
    public EnemySpawner spawner;
    public Transform player;

    public DoorController[] doors;

    private int currentKills = 0;
    private bool arenaActive = false;
    private bool finalEnemySpawned = false;
    private bool arenaCleared = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!arenaActive && other.CompareTag("Player"))
        {
            StartArena();
        }
    }

    void StartArena()
    {
        arenaActive = true;

        foreach (DoorController door in doors)
            door.CloseDoor();

        if (LevelManager.Instance.currentLevelType == LevelType.Boss)
            {
                spawner.SpawnBoss();
            }
            else
            {
                spawner.StartSpawning(this);
            }
    }

    public void RegisterKill(bool isFinalEnemy)
    {
        if (arenaCleared) return;

        if (LevelManager.Instance != null && LevelManager.Instance.IsKillAndCollectLevel())
        {
            return;
        }

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
    }
}
