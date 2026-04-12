using UnityEngine;

/// <summary>
/// BossHealthBridge
///
/// Place on BlobBoss root alongside InfectionBlobBoss.
///
/// Health.cs lives on the BodyHitbox child. When the boss's HP hits 0,
/// Health calls Die() which looks for an Enemy component — the boss has
/// none, so without this bridge the BodyHitbox child gets Destroyed and
/// the arena doors never open.
///
/// This bridge subscribes to Health.OnHealthChanged and intercepts the
/// death, calling OnBossDeath() on the Boss component instead.
/// </summary>
public class BossHealthBridge : MonoBehaviour
{
    [Tooltip("Drag the BodyHitbox child's Health component here.")]
    [SerializeField] private Health bossHealth;

    private Boss boss;
    private bool deathTriggered = false;

    private void Awake()
    {
        boss = GetComponent<Boss>();

        if (boss == null)
            Debug.LogError("BossHealthBridge: no Boss component on " + gameObject.name, this);

        if (bossHealth == null)
            Debug.LogError("BossHealthBridge: bossHealth not assigned on " + gameObject.name, this);
    }

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        if (deathTriggered || bossHealth == null || boss == null) return;

        if (bossHealth.currentHealth <= 0)
        {
            deathTriggered = true;
            boss.OnBossDeath();
        }
    }
}