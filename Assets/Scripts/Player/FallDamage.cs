using UnityEngine;

public class FallDamage : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GroundCheck component on the player.")]
    [SerializeField] private GroundCheck groundCheck;

    [Tooltip("The Health component on the player.")]
    [SerializeField] private Health health;

    [Header("Fall Damage Settings")]
    [Tooltip("Minimum fall distance in Unity units before damage is applied.")]
    [SerializeField] private float minimumFallDistance = 4f;

    [Tooltip("Damage dealt per Unity unit fallen beyond the minimum.")]
    [SerializeField] private float damagePerUnit = 1f;

    [Tooltip("Optional effect spawned on a damaging landing.")]
    [SerializeField] private GameObject hardLandingEffect;

    [Header("Wall Jump Immunity")]
    [Tooltip("Seconds after a wall jump during which fall damage is suppressed.")]
    [SerializeField] private float wallJumpImmunityDuration = 0.6f;

    //  Internal state
    private Rigidbody2D rb;

    private bool  isFalling         = false;   // true once vy < 0 while airborne
    private float fallStartY        = 0f;      // Y recorded when downward movement began
    private bool  wasGrounded       = true;
    private float wallJumpImmunityTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (groundCheck == null)
            Debug.LogWarning("FallDamage: groundCheck not assigned.", this);
        if (health == null)
            Debug.LogWarning("FallDamage: health not assigned.", this);
    }

    private void Update()
    {
        if (wallJumpImmunityTimer > 0f)
            wallJumpImmunityTimer -= Time.deltaTime;

        bool grounded = groundCheck != null && groundCheck.CheckGrounded();

        if (grounded)
        {
            if (!wasGrounded && isFalling)
                OnLanded();

            isFalling   = false;
            wasGrounded = true;
            return;
        }

        // Airborne
        wasGrounded = false;

        float vy = rb != null ? rb.velocity.y : 0f;

        if (!isFalling && vy < -0.1f)
        {
            // Player has started moving downward — begin measuring from here
            isFalling  = true;
            fallStartY = transform.position.y;
        }
        else if (isFalling && vy >= 0f)
        {
            // Velocity went back upward mid-air (e.g. double jump off apex)
            // Reset so we measure from the new peak instead
            isFalling = false;
        }
    }

    private void OnLanded()
    {
        // Ignore if still within wall jump immunity window
        if (wallJumpImmunityTimer > 0f) return;

        float fallDistance = fallStartY - transform.position.y;

        if (fallDistance < minimumFallDistance) return;

        float excessDistance = fallDistance - minimumFallDistance;
        int damage = Mathf.CeilToInt(excessDistance * damagePerUnit);

        if (damage <= 0) return;

        if (health != null)
            health.TakeDamage(damage);

        if (hardLandingEffect != null)
            Instantiate(hardLandingEffect, transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Call this from PlayerController whenever a wall jump is performed.
    /// Grants a short immunity window so the upward arc and any brief
    /// downward dip between wall jumps never count as a fall.
    /// </summary>
    public void NotifyWallJump()
    {
        wallJumpImmunityTimer = wallJumpImmunityDuration;
        // Reset fall tracking — the wall jump launches upward,
        // so any previous fall measurement is no longer valid
        isFalling = false;
    }
}