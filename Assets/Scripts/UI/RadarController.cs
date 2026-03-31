using UnityEngine;
using System.Collections.Generic;

public class RadarController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public RectTransform blipContainer;
    public GameObject enemyBlipPrefab;
    public GameObject playerBlipPrefab;
    private RectTransform playerBlip;

    [Header("Radar Ping Settings")]
    public GameObject radarPingPrefab;
    public float pingInterval = 2f;
    private float pingTimer;

    [Header("Settings")]
    public float radarRange = 20f;

    private Dictionary<Enemy, GameObject> enemyBlips = new Dictionary<Enemy, GameObject>();
    void Start()
    {
        GameObject pb = Instantiate(playerBlipPrefab, blipContainer);
        playerBlip = pb.GetComponent<RectTransform>();
    }
    void Update()
    {
        pingTimer += Time.deltaTime;
        if (pingTimer >= pingInterval)
        {
            SpawnPing();
            pingTimer = 0f;
        }
        UpdateRadar();
    }

    void UpdateRadar()
    {
        List<Enemy> toRemove = new List<Enemy>();

        playerBlip.anchoredPosition = Vector2.zero;

        foreach (var pair in enemyBlips)
        {
            if (pair.Key == null)
            {
                Destroy(pair.Value);
                toRemove.Add(pair.Key);
            }
        }

        foreach (var enemy in toRemove)
        {
            enemyBlips.Remove(enemy);
        }

        foreach (Enemy enemy in Enemy.ActiveEnemies)
        {
            if (!enemyBlips.ContainsKey(enemy))
            {
                GameObject blip = Instantiate(enemyBlipPrefab, blipContainer);
                enemyBlips.Add(enemy, blip);
            }
        }

        foreach (var pair in enemyBlips)
        {
            Enemy enemy = pair.Key;
            GameObject blip = pair.Value;

            if (enemy == null) continue;

            Vector3 offset = enemy.transform.position - player.position;

            float distance = offset.magnitude;

            if (distance > radarRange)
            {
                offset = offset.normalized * radarRange;
            }

            float radarRadius = blipContainer.rect.width / 2;

            Vector2 pos = new Vector2(offset.x, offset.y) / radarRange * radarRadius;

            blip.GetComponent<RectTransform>().anchoredPosition = pos;

        }
    }
    void SpawnPing()
    {
        GameObject ping = Instantiate(radarPingPrefab, blipContainer);
        RectTransform rect = ping.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
    }
}