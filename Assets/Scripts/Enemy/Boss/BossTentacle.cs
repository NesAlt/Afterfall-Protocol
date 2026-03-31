using System.Collections;
using UnityEngine;

/// <summary>
/// A single infection tentacle spawned by InfectionBlobBoss.
///
/// The tentacle extends in a direction, lingers briefly while dealing
/// damage on contact, then retracts and destroys itself.
///
/// Setup: Attach a Damage component to this GameObject (or a child).
/// The SpriteRenderer's initial localScale.x is used as the retracted size;
/// maxLength controls how far it extends.
/// </summary>
[RequireComponent(typeof(Damage))]
public class BossTentacle : MonoBehaviour
{
    [Header("Shape & Travel")]
    [Tooltip("World units the tentacle reaches at full extension")]
    [SerializeField] private float maxLength = 3.5f;

    [Tooltip("Time (seconds) to reach full extension")]
    [SerializeField] private float extendTime = 0.25f;

    [Tooltip("How long the tentacle stays fully extended")]
    [SerializeField] private float lingerTime = 0.4f;

    [Tooltip("Time (seconds) to fully retract")]
    [SerializeField] private float retractTime = 0.3f;

    [Header("Optional Effects")]
    [SerializeField] private GameObject impactEffect;   // spawned at tip on full extension

    // ---------------------------------------------------------------
    //  Internal
    // ---------------------------------------------------------------
    private Vector2 launchDirection;
    private SpriteRenderer sr;
    private Collider2D col;
    private Vector3 originalScale;
    private bool launched = false;

    // ---------------------------------------------------------------
    //  Initialisation
    // ---------------------------------------------------------------
    private void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null)
            originalScale = sr.transform.localScale;

        // Damage should not destroy itself — the tentacle manages its lifetime
        Damage dmg = GetComponent<Damage>();
        if (dmg != null)
            dmg.destroyAfterDamage = false;

        // Start with collider off until fully extended
        if (col != null)
            col.enabled = false;
    }

    /// <summary>
    /// Called by InfectionBlobBoss immediately after instantiation.
    /// </summary>
    public void Launch(Vector2 direction)
    {
        launchDirection = direction.normalized;
        launched = true;

        // Rotate the sprite to face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 if sprite points up

        StartCoroutine(TentacleLifetime());
    }

    // ---------------------------------------------------------------
    //  Lifetime Coroutine
    // ---------------------------------------------------------------
    private IEnumerator TentacleLifetime()
    {
        // Phase 1: Extend
        yield return StartCoroutine(ScaleLength(0f, maxLength, extendTime));

        if (impactEffect != null)
        {
            Vector3 tip = transform.position + (Vector3)(launchDirection * maxLength);
            Instantiate(impactEffect, tip, Quaternion.identity);
        }

        // Enable collision at full extension
        if (col != null)
            col.enabled = true;

        // Phase 2: Linger
        yield return new WaitForSeconds(lingerTime);

        // Phase 3: Retract
        if (col != null)
            col.enabled = false;

        yield return StartCoroutine(ScaleLength(maxLength, 0f, retractTime));

        Destroy(gameObject);
    }

    // ---------------------------------------------------------------
    //  Scale helper — stretches the sprite along its local Y axis
    //  (adjust to X if your sprite is horizontally oriented)
    // ---------------------------------------------------------------
    private IEnumerator ScaleLength(float fromLength, float toLength, float duration)
    {
        if (sr == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float length = Mathf.Lerp(fromLength, toLength, t);

            // Stretch along Y (the "up" axis of the rotated sprite)
            Vector3 scale     = originalScale;
            scale.y           = length;
            sr.transform.localScale = scale;

            // Move the pivot so the base stays at the boss and tip extends outward
            // (only needed if your sprite pivot is at its centre — shift it to the base in the prefab instead)

            yield return null;
        }

        Vector3 finalScale  = originalScale;
        finalScale.y        = toLength;
        sr.transform.localScale = finalScale;
    }

    // ---------------------------------------------------------------
    //  Continuous damage tick while fully extended (optional)
    // ---------------------------------------------------------------
    private void OnTriggerStay2D(Collider2D other)
    {
        // Damage component handles the hit; this is just a safety log hook.
        // The Damage component on this object uses dealDamageOnTriggerEnter.
    }
}
