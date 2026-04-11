using UnityEngine;

/// <summary>
/// BossHealthBridge
///
/// Sits on the BossRoot alongside InfectionBlobBoss.
///
/// Health lives on the BodyHitbox child, not on the root, so this bridge
/// takes a direct serialized reference to that child's Health component
/// rather than using GetComponent on itself.
///
/// When Health reaches 0, it calls Die() which looks for an Enemy component —
/// the boss has none, so it would just Destroy the BodyHitbox child.
/// This bridge intercepts OnHealthChanged first and calls OnBossDeath()
/// cleanly before that happens.
/// </summary>
public class BossHealthBridge : MonoBehaviour
{
    [Tooltip("Drag the BodyHitbox child's Health component here.")]
    [SerializeField] private Health bodyHitboxHealth;

    private Boss boss;
    private bool deathTriggered = false;

    private void Awake()
    {
        boss = GetComponent<Boss>();

        if (boss == null)
            Debug.LogError("BossHealthBridge: No Boss component on " + gameObject.name, this);

        if (bodyHitboxHealth == null)
            Debug.LogError("BossHealthBridge: bodyHitboxHealth not assigned on " + gameObject.name, this);
    }

    private void OnEnable()
    {
        if (bodyHitboxHealth != null)
            bodyHitboxHealth.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (bodyHitboxHealth != null)
            bodyHitboxHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        if (deathTriggered || bodyHitboxHealth == null || boss == null) return;

        if (bodyHitboxHealth.currentHealth <= 0)
        {
            deathTriggered = true;
            boss.OnBossDeath();
        }
    }
}