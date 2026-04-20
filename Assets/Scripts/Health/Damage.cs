using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Damage : MonoBehaviour
{
    [Header("Team Settings")]
    [Tooltip("The team associated with this damage")]
    public int teamId = 0;

    [Header("Damage Settings")]
    [Tooltip("How much damage to deal")]
    public float damageAmount = 1;
    [Tooltip("Prefab to spawn after doing damage")]
    public GameObject hitEffect = null;
    [Tooltip("Whether or not to destroy the attached game object after dealing damage")]
    public bool destroyAfterDamage = true;
    [Tooltip("Whether or not to apply damage when triggers collide")]
    public bool dealDamageOnTriggerEnter = false;
    [Tooltip("Whether or not to apply damage when triggers stay, for damage over time")]
    public bool dealDamageOnTriggerStay = false;
    [Tooltip("Whether or not to apply damage on non-trigger collider collisions")]
    public bool dealDamageOnCollision = false;

    void Start()
    {
        // Enemy bullets/attacks scale up with corruption
        if (teamId == 1 && LevelManager.Instance != null)
        {
            float mult  = LevelManager.Instance.GetEnemyDamageMultiplier();
            damageAmount = Mathf.RoundToInt(damageAmount * mult);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dealDamageOnTriggerEnter)
            DealDamage(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (dealDamageOnTriggerStay)
            DealDamage(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (dealDamageOnCollision)
            DealDamage(collision.gameObject);
    }

    private void DealDamage(GameObject collisionGameObject)
    {
        Health collidedHealth = collisionGameObject.GetComponent<Health>();
        if (collidedHealth != null && collidedHealth.teamId != this.teamId)
        {
            collidedHealth.TakeDamage(Mathf.RoundToInt(damageAmount));
            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, transform.rotation, null);
            if (destroyAfterDamage)
                Destroy(this.gameObject);
        }
    }
}