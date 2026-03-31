using System.Collections;
using UnityEngine;

/// <summary>
/// Infection Blob Boss — a semi-circular octopus-like enemy that slithers
/// along surfaces and attacks with directional or burst tentacles.
///
/// Attach to the boss root GameObject alongside a Health component and
/// a BossHealthBridge component.
/// </summary>
public class BlobBoss : Boss
{
    // ---------------------------------------------------------------
    //  State Machine
    // ---------------------------------------------------------------
    private enum BossState { Idle, Moving, DirectionalAttack, BurstAttack, Dead }
    private BossState currentState = BossState.Idle;

    // ---------------------------------------------------------------
    //  Movement
    // ---------------------------------------------------------------
    [Header("Movement")]
    [Tooltip("Horizontal move speed along the surface")]
    [SerializeField] private float moveSpeed = 1.8f;

    [Tooltip("Amplitude of the vertical sinusoidal \"slither\" bob")]
    [SerializeField] private float slitherAmplitude = 0.08f;

    [Tooltip("Frequency of the slither bob")]
    [SerializeField] private float slitherFrequency = 3f;

    [Tooltip("How long to move before re-evaluating direction (seconds)")]
    [SerializeField] private float moveDuration = 2.5f;

    [Tooltip("Ground check ray length — tune to match sprite pivot")]
    [SerializeField] private float groundRayLength = 0.6f;

    [Tooltip("Layer mask for the floor")]
    [SerializeField] private LayerMask groundLayer;

    // ---------------------------------------------------------------
    //  Attack — Directional Tentacles
    // ---------------------------------------------------------------
    [Header("Directional Attack")]
    [Tooltip("Tentacle prefab (see BossTentacle.cs)")]
    [SerializeField] private GameObject tentaclePrefab;

    [Tooltip("Number of tentacles fired in a directional attack")]
    [SerializeField] private int directionalTentacleCount = 3;

    [Tooltip("Spread angle (degrees) between adjacent tentacles")]
    [SerializeField] private float directionalSpread = 18f;

    [Tooltip("Seconds between directional attacks")]
    [SerializeField] private float directionalAttackCooldown = 3.5f;

    // ---------------------------------------------------------------
    //  Attack — Burst (360°)
    // ---------------------------------------------------------------
    [Header("Burst Attack")]
    [Tooltip("Number of tentacles spawned around the full circle")]
    [SerializeField] private int burstTentacleCount = 8;

    [Tooltip("Seconds between burst attacks")]
    [SerializeField] private float burstAttackCooldown = 8f;

    [Tooltip("Warning hold time before the burst fires")]
    [SerializeField] private float burstWindupTime = 1.2f;

    // ---------------------------------------------------------------
    //  Phase 2 (sub-50% health)
    // ---------------------------------------------------------------
    [Header("Phase 2 Escalation")]
    [Tooltip("Health fraction at which phase 2 begins (0–1)")]
    [SerializeField] private float phase2Threshold = 0.5f;

    [Tooltip("Speed multiplier applied in phase 2")]
    [SerializeField] private float phase2SpeedMultiplier = 1.6f;

    [Tooltip("Cooldown multiplier applied to attacks in phase 2 (< 1 = faster)")]
    [SerializeField] private float phase2CooldownMultiplier = 0.65f;

    // ---------------------------------------------------------------
    //  VFX / Audio hooks
    // ---------------------------------------------------------------
    [Header("Effects")]
    [SerializeField] private GameObject burstWarningEffect;   // plays during windup
    [SerializeField] private GameObject deathExplosionEffect;

    // ---------------------------------------------------------------
    //  Internal
    // ---------------------------------------------------------------
    private Transform playerTransform;
    private Rigidbody2D rb;
    private Health health;
    private SpriteRenderer spriteRenderer;

    private float nextDirectionalAttackTime;
    private float nextBurstAttackTime;
    private float moveTimer;
    private int moveDirection = 1;          // +1 right, -1 left
    private bool isPhase2 = false;
    private float slitherTime = 0f;
    private Vector3 baseLocalPosition;

    // ---------------------------------------------------------------
    //  Initialisation
    // ---------------------------------------------------------------
    public override void Initialize(ArenaController arenaController)
    {
        base.Initialize(arenaController);

        rb              = GetComponent<Rigidbody2D>();
        health          = GetComponent<Health>();
        spriteRenderer  = GetComponent<SpriteRenderer>();

        // Stagger the first attacks so they don't land simultaneously
        nextDirectionalAttackTime = Time.time + 2f;
        nextBurstAttackTime       = Time.time + burstAttackCooldown;

        baseLocalPosition = transform.localPosition;
        PickNewMoveDirection();
        StartCoroutine(BossLoop());
    }

    /// <summary>Called by BossHealthBridge when Health reaches 0.</summary>
    public override void OnBossDeath()
    {
        if (currentState == BossState.Dead) return;
        currentState = BossState.Dead;
        StopAllCoroutines();

        if (deathExplosionEffect != null)
            Instantiate(deathExplosionEffect, transform.position, Quaternion.identity);

        arena.ForceEndArena();
        Destroy(gameObject, 0.1f);
    }

    // ---------------------------------------------------------------
    //  Main Loop
    // ---------------------------------------------------------------
    private IEnumerator BossLoop()
    {
        while (currentState != BossState.Dead)
        {
            CheckPhaseTransition();

            // Attack windows take priority over movement
            if (Time.time >= nextBurstAttackTime)
            {
                yield return StartCoroutine(DoBurstAttack());
            }
            else if (Time.time >= nextDirectionalAttackTime && playerTransform != null)
            {
                yield return StartCoroutine(DoDirectionalAttack());
            }
            else
            {
                currentState = BossState.Moving;
                yield return null;      // movement handled in FixedUpdate
            }
        }
    }

    // ---------------------------------------------------------------
    //  Physics Update — surface slither movement
    // ---------------------------------------------------------------
    private void FixedUpdate()
    {
        if (currentState != BossState.Moving) return;

        SlitherAlongGround();
    }

    private void SlitherAlongGround()
    {
        float speed = moveSpeed * (isPhase2 ? phase2SpeedMultiplier : 1f);

        // Horizontal movement
        Vector2 velocity = rb != null
            ? rb.velocity
            : Vector2.zero;

        velocity.x = moveDirection * speed;

        // Keep the boss hugging the floor via a downward raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            groundRayLength,
            groundLayer);

        if (hit.collider != null)
        {
            // Gently correct vertical position to stay on surface
            float targetY = hit.point.y + (groundRayLength * 0.5f);
            velocity.y    = (targetY - transform.position.y) * 10f;
        }

        if (rb != null)
            rb.velocity = velocity;

        // Sinusoidal body bob — the "slithering" feel
        slitherTime += Time.fixedDeltaTime * slitherFrequency;
        Vector3 pos    = transform.position;
        pos.y         += Mathf.Sin(slitherTime) * slitherAmplitude;
        transform.position = pos;

        // Flip sprite to face movement direction
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveDirection < 0;

        // Count down move duration
        moveTimer -= Time.fixedDeltaTime;
        if (moveTimer <= 0f)
            PickNewMoveDirection();
    }

    private void PickNewMoveDirection()
    {
        // Bias toward the player if available, otherwise random
        if (playerTransform != null)
        {
            float diff = playerTransform.position.x - transform.position.x;
            moveDirection = diff >= 0 ? 1 : -1;
        }
        else
        {
            moveDirection = Random.value > 0.5f ? 1 : -1;
        }
        moveTimer = moveDuration;
    }

    // ---------------------------------------------------------------
    //  Directional Attack
    // ---------------------------------------------------------------
    private IEnumerator DoDirectionalAttack()
    {
        currentState = BossState.DirectionalAttack;

        // Brief pause — telegraphs the attack
        yield return new WaitForSeconds(0.3f);

        if (playerTransform != null && tentaclePrefab != null)
        {
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            float   baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // Fire an odd number of tentacles centred on the player direction
            int    count  = directionalTentacleCount;
            float  offset = -(count - 1) * 0.5f * directionalSpread;

            for (int i = 0; i < count; i++)
            {
                float   angle   = baseAngle + offset + i * directionalSpread;
                Vector2 dir     = AngleToDir(angle);
                SpawnTentacle(dir);
            }
        }

        float cooldown = directionalAttackCooldown * (isPhase2 ? phase2CooldownMultiplier : 1f);
        nextDirectionalAttackTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.5f);
        currentState = BossState.Moving;
    }

    // ---------------------------------------------------------------
    //  Burst Attack
    // ---------------------------------------------------------------
    private IEnumerator DoBurstAttack()
    {
        currentState = BossState.BurstAttack;

        // Windup — show a warning effect
        if (burstWarningEffect != null)
        {
            GameObject warning = Instantiate(burstWarningEffect,
                transform.position, Quaternion.identity, transform);
            Destroy(warning, burstWindupTime);
        }

        yield return new WaitForSeconds(burstWindupTime);

        // Fire tentacles evenly around 360°
        if (tentaclePrefab != null)
        {
            float angleStep = 360f / burstTentacleCount;
            for (int i = 0; i < burstTentacleCount; i++)
            {
                float   angle = i * angleStep;
                Vector2 dir   = AngleToDir(angle);
                SpawnTentacle(dir);
            }
        }

        float cooldown = burstAttackCooldown * (isPhase2 ? phase2CooldownMultiplier : 1f);
        nextBurstAttackTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.8f);
        currentState = BossState.Moving;
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------
    private void SpawnTentacle(Vector2 direction)
    {
        GameObject t = Instantiate(tentaclePrefab, transform.position, Quaternion.identity);
        BossTentacle tentacle = t.GetComponent<BossTentacle>();
        if (tentacle != null)
            tentacle.Launch(direction);
    }

    private static Vector2 AngleToDir(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void CheckPhaseTransition()
    {
        if (isPhase2 || health == null) return;
        float fraction = (float)health.currentHealth / health.maximumHealth;
        if (fraction <= phase2Threshold)
        {
            isPhase2 = true;
            OnEnterPhase2();
        }
    }

    private void OnEnterPhase2()
    {
        // Immediately queue an extra burst to announce the phase
        nextBurstAttackTime       = Time.time + 1f;
        nextDirectionalAttackTime = Time.time + 2f;
        // Visual cue — tint the sprite
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.9f, 0.4f, 1f);
    }

    // ---------------------------------------------------------------
    //  Player reference setter (called by EnemySpawner / ArenaController)
    // ---------------------------------------------------------------
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }
}
