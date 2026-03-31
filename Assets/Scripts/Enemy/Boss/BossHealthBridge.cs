using UnityEngine;

/// <summary>
/// BossHealthBridge
///
/// Sits on the boss GameObject alongside Health.cs and a Boss subclass.
///
/// Problem it solves:
///   Health.cs calls Enemy.Die() or Destroy() on death. Bosses don't have
///   an Enemy component, so without this bridge the boss would simply be
///   destroyed without triggering OnBossDeath() — meaning the arena would
///   never open its doors.
///
/// How it works:
///   Hooks into Health.OnHealthChanged and calls the Boss's OnBossDeath()
///   the first time currentHealth drops to 0.
/// </summary>
[RequireComponent(typeof(Health))]
public class BossHealthBridge : MonoBehaviour
{
    private Health health;
    private Boss   boss;
    private bool   deathTriggered = false;

    private void Awake()
    {
        health = GetComponent<Health>();
        boss   = GetComponent<Boss>();

        if (health == null)
            Debug.LogError("BossHealthBridge: No Health component found on " + gameObject.name);

        if (boss == null)
            Debug.LogError("BossHealthBridge: No Boss component found on " + gameObject.name);
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        if (deathTriggered) return;
        if (health == null || boss == null) return;

        if (health.currentHealth <= 0)
        {
            deathTriggered = true;
            boss.OnBossDeath();
        }
    }
}
