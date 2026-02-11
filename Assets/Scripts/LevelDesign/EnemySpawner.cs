using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject finalEnemyPrefab;

    public Transform[] spawnPoints;
    public float spawnInterval = 2f;
    public int maxAliveEnemies = 3;

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
    }

    public void SpawnFinalEnemy()
    {
        StopSpawning();
        SpawnEnemy(finalEnemyPrefab, true);
    }

    public void NotifyEnemyDeath()
    {
        aliveEnemies--;
    }
}
