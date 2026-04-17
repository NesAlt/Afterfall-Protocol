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
        if (!CompareTag("Player") && LevelManager.Instance != null)
        {
            float mult = LevelManager.Instance.GetEnemyHealthMultiplier();
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
        {
            isInIFrames = false;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvincible) return;

        if (isInIFrames || currentHealth <= 0) return;

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation, null);
        }

        isInIFrames = true;
        timeToBecomeDamagableAgain = Time.time + invincibilityTime;

        currentHealth -= damageAmount;

        Debug.Log("Damage on: " + gameObject.name);
        Debug.Log("Health changed to: " + currentHealth);

        OnHealthChanged?.Invoke();
        CheckDeath();

        GameManager.UpdateUIElements();
    }

    public void ReceiveHealing(int healingAmount)
    {
        currentHealth += healingAmount;
        if (currentHealth > maximumHealth)
        {
            currentHealth = maximumHealth;
        }

        OnHealthChanged?.Invoke();
        CheckDeath();

        GameManager.UpdateUIElements();
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
        {
            Instantiate(deathEffect, transform.position, transform.rotation, null);
        }

        if (CompareTag("Player"))
        {
            PlayerDeathController deathController =
                GetComponent<PlayerDeathController>();

            if (deathController != null)
            {
                deathController.HandleDeath();
            }
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
                {
                    boss.OnBossDeath();
                }
                else
                {
                    Destroy(this.gameObject);
                }
            }   
        }

        GameManager.UpdateUIElements();
    }
    public void GameOver()
    {
        if (GameManager.instance != null && gameObject.tag == "Player")
        {
            GameManager.instance.GameOver();
        }
    }
}
