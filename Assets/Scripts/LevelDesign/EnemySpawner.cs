using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject finalEnemyPrefab;
    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 2f;
    public int maxAliveEnemies = 3;
    [Header("Spawn Limit")]
    public int maxTotalEnemies = 20;
    private int totalSpawned = 0;

    private ArenaController arena;
    private bool spawningActive = false;
    private int aliveEnemies = 0;

    public void StartSpawning(ArenaController arenaController)
    {
        arena = arenaController;
        spawningActive = true;
        StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        spawningActive = false;
        StopAllCoroutines();
    }

    IEnumerator SpawnRoutine()
    {
        while (spawningActive)
        {
            if (LevelManager.Instance != null && 
                LevelManager.Instance.IsKillAndCollectLevel() &&
                totalSpawned >= maxTotalEnemies)
            {
                spawningActive = false;
                yield break;
            }

            if (aliveEnemies < maxAliveEnemies)
            {
                SpawnEnemy(enemyPrefab, false);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy(GameObject prefab, bool isFinal)
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(prefab, point.position, Quaternion.identity);

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        enemyScript.Initialize(arena, this, isFinal,arena.player);

        aliveEnemies++;
        totalSpawned++;
    }

    public void SpawnFinalEnemy()
    {
        StopSpawning();
        SpawnEnemy(finalEnemyPrefab, true);
    }
    public void SpawnBoss()
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject bossObj = Instantiate(bossPrefab, point.position, Quaternion.identity);

        Boss boss = bossObj.GetComponent<Boss>();
        boss.Initialize(arena);
    }
    public void NotifyEnemyDeath()
    {
        aliveEnemies--;
    }
}
