using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject finalEnemyPrefab;
    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public SpawnPoint[] spawnPoints;
    public float spawnInterval = 2f;
    public int maxAliveEnemies = 3;
    [Header("Spawn Limit")]
    public int maxTotalEnemies = 20;
    private int totalSpawned = 0;
    [Header("Per Point Control")]
    public int maxAlivePerPoint = 2;
    [Header("Anti-Camping")]
    public float minDistanceFromPlayer = 5f;

    private ArenaController arena;
    private bool spawningActive = false;
    private int aliveEnemies = 0;

    public void StartSpawning(ArenaController arenaController)
    {
        arena = arenaController;
        spawningActive = true;
        totalSpawned = 0;
        aliveEnemies = 0;

        foreach (SpawnPoint point in spawnPoints)
        {
            point.aliveCount = 0;
        }
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
                SpawnPoint point = GetNextSpawnPoint();

                if (point != null)
                {
                    SpawnEnemyAtPoint(enemyPrefab, false, point);
                }            
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemyAtPoint(GameObject prefab, bool isFinal, SpawnPoint point)
    {
        GameObject enemy = Instantiate(prefab, point.transform.position, Quaternion.identity);

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        // Pass the point's drop multiplier to the enemy
        enemyScript.Initialize(arena, this, isFinal, arena.player, point, point.dropChanceMultiplier);

        aliveEnemies++;
        point.aliveCount++;
        totalSpawned++;
    }
    SpawnPoint GetNextSpawnPoint()
    {
        List<SpawnPoint> validPoints = new List<SpawnPoint>();

        foreach (var point in spawnPoints)
        {
            if (point.aliveCount >= maxAlivePerPoint) continue;

            if (arena != null && arena.player != null)
            {
                float dist = Vector2.Distance(point.transform.position, arena.player.position);
                if (dist < minDistanceFromPlayer) continue;
            }
            validPoints.Add(point);
        }

        if (validPoints.Count == 0) return null;

        return GetHeightWeightedPoint(validPoints);
    }

    SpawnPoint GetHeightWeightedPoint(List<SpawnPoint> points)
    {
        float minY = float.MaxValue;
        foreach (var p in points)
            if (p.transform.position.y < minY) minY = p.transform.position.y;

        float totalWeight = 0f;
        List<float> weights = new List<float>();

        foreach (var p in points)
        {
            float w = (p.transform.position.y - minY) + 1f;
            weights.Add(w);
            totalWeight += w;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return points[i];
        }

        return points[points.Count - 1];
    }
    public void SpawnFinalEnemy()
    {
        StopSpawning();
        SpawnPoint point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        SpawnEnemyAtPoint(finalEnemyPrefab, true, point);
    }
    public void SpawnBoss()
    {
        SpawnPoint point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject bossObj = Instantiate(bossPrefab, point.transform.position, Quaternion.identity);

        Boss boss = bossObj.GetComponent<Boss>();
        boss.Initialize(arena);
    }
    public void NotifyEnemyDeath(SpawnPoint point)
    {
        aliveEnemies--;
        if (point != null)
        {
            point.aliveCount = Mathf.Max(0, point.aliveCount - 1);
        }
    }
}
