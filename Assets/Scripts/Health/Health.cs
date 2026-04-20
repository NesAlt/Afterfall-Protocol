using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Team Settings")]
    [Tooltip("The team associated with this damage")]
    public int teamId = 0;

    [Header("Health Settings")]
    [Tooltip("The default health value")]
    public int defaultHealth = 1;
    [Tooltip("The maximum health value")]
    public int maximumHealth = 1;
    [Tooltip("The current in game health value")]
    public int currentHealth = 1;
    [Tooltip("Invulnerability duration, in seconds, after taking damage")]
    public float invincibilityTime = .3f;

    [Header("Invincibility - Testing")]
    [Tooltip("If true, this object will not take any damage")]
    public bool isInvincible = false;
    private float timeToBecomeDamagableAgain = 0;
    private bool isInIFrames = false;

    [Tooltip("Healthbar event")]
    public System.Action OnHealthChanged;

    void Start()
    {
        if (CompareTag("Player"))
        {
            // Apply flat health bonus from buffs earned this run
            int bonus = PlayerBuffManager.Instance != null
                ? (int)PlayerBuffManager.Instance.HealthBonus : 0;

            defaultHealth  += bonus;
            maximumHealth  += bonus;
            currentHealth   = defaultHealth;
        }

        if (!CompareTag("Player") && LevelManager.Instance != null)
        {
            float mult    = LevelManager.Instance.GetEnemyHealthMultiplier();
            maximumHealth = Mathf.RoundToInt(maximumHealth * mult);
            currentHealth = Mathf.RoundToInt(defaultHealth * mult);
            defaultHealth = currentHealth;
        }
    }

    void Update()
    {
        InvincibilityCheck();
    }

    private void InvincibilityCheck()
    {
        if (isInIFrames && timeToBecomeDamagableAgain <= Time.time)
            isInIFrames = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isInvincible) return;
        if (isInIFrames || currentHealth <= 0) return;

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, transform.rotation, null);

        isInIFrames = true;
        timeToBecomeDamagableAgain = Time.time + invincibilityTime;

        currentHealth = Mathf.Max(currentHealth - Mathf.RoundToInt(damageAmount), 0);

        OnHealthChanged?.Invoke();
        CheckDeath();
    }

    public void ReceiveHealing(int healingAmount)
    {
        currentHealth += healingAmount;
        if (currentHealth > maximumHealth)
            currentHealth = maximumHealth;

        OnHealthChanged?.Invoke();
        CheckDeath();
    }
    public void ApplyHealthBuff(int bonus)
    {
        int oldMax = maximumHealth;

        maximumHealth += bonus;
        defaultHealth += bonus;

        currentHealth += bonus;

        currentHealth = Mathf.Min(currentHealth, maximumHealth);

        OnHealthChanged?.Invoke();

        Debug.Log($"[Health] Buff applied. Max: {maximumHealth}, Current: {currentHealth}");
    }

    [Header("Effects & Polish")]
    [Tooltip("The effect to create when this health dies")]
    public GameObject deathEffect;
    [Tooltip("The effect to create when this health is damaged (but does not die)")]
    public GameObject hitEffect;

    bool CheckDeath()
    {
        if (currentHealth <= 0)
        {
            Die();
            return true;
        }
        return false;
    }

    void Die()
    {
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, transform.rotation, null);

        if (CompareTag("Player"))
        {
            PlayerDeathController deathController = GetComponent<PlayerDeathController>();
            if (deathController != null)
                deathController.HandleDeath();
        }
        else
        {
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
            else
            {
                Boss boss = GetComponent<Boss>();
                if (boss != null)
                    boss.OnBossDeath();
                else
                    Destroy(this.gameObject);
            }
        }
    }
}