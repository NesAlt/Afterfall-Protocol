using System.Collections;
using UnityEngine;

public class BossTentacle : MonoBehaviour
{
    [Header("Child Reference")]
    [Tooltip("The child GameObject that holds SpriteRenderer, Animator, Collider2D, and Damage.")]
    [SerializeField] private Transform visual;

    [Header("Timing")]
    [Tooltip("Time in seconds to reach full extension.")]
    [SerializeField] private float extendTime = 0.5f;

    [Tooltip("How long the tentacle stays fully extended and active.")]
    [SerializeField] private float lingerTime = 0.4f;

    [Tooltip("Time in seconds to fully retract.")]
    [SerializeField] private float retractTime = 0.3f;

    // Retract trigger — fires just before scaling back down
    private static readonly int AnimRetract = Animator.StringToHash("Retract");

    // Internal references pulled from Visual child
    private Collider2D  hitCollider;
    private Animator    animator;

    private void Awake()
    {
        if (visual == null)
        {
            Debug.LogError("BossTentacle: Visual child not assigned on " + gameObject.name, this);
            return;
        }

        hitCollider = visual.GetComponent<Collider2D>();
        animator    = visual.GetComponent<Animator>();

        // Collider off until fully extended
        if (hitCollider != null)
            hitCollider.enabled = false;

        // Start flat — scale Y to 0, keep X and Z at 1
        visual.localScale = new Vector3(1f, 0f, 1f);
    }

    /// <summary>
    /// Called by InfectionBlobBoss immediately after Instantiate.
    /// Rotates the tentacle to face the given direction and starts the lifetime.
    /// </summary>
    public void Launch(Vector2 direction)
    {
        // Rotate root so the Visual child's local Up points in the launch direction
        // (works because the sprite is drawn pointing upward with pivot at bottom)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        StartCoroutine(TentacleLifetime());
    }

    private IEnumerator TentacleLifetime()
    {
        // --- Extend ---
        // Scale Y from 0 to 1 over extendTime.
        // The Animator's Extend state plays the 4-frame animation in parallel.
        yield return StartCoroutine(ScaleY(0f, 1f, extendTime));

        // Animator auto-transitions to Linger via Has Exit Time when Extend finishes

        // Enable hitbox now that the tentacle is fully out
        if (hitCollider != null)
            hitCollider.enabled = true;

        // --- Linger ---
        yield return new WaitForSeconds(lingerTime);

        // Disable hitbox before retracting
        if (hitCollider != null)
            hitCollider.enabled = false;

        // --- Retract ---
        if (animator != null)
            animator.SetTrigger(AnimRetract);

        yield return StartCoroutine(ScaleY(1f, 0f, retractTime));

        Destroy(gameObject);
    }

    /// <summary>
    /// Smoothly scales the Visual child's local Y between two values.
    /// X and Z are left at 1 so the sprite width is unchanged.
    /// Because the CapsuleCollider2D is on Visual, it resizes automatically.
    /// </summary>
    private IEnumerator ScaleY(float from, float to, float duration)
    {
        if (visual == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            visual.localScale = new Vector3(1f, Mathf.Lerp(from, to, t), 1f);
            yield return null;
        }

        visual.localScale = new Vector3(1f, to, 1f);
    }
}