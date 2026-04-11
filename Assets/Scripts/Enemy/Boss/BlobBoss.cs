using System.Collections;
using UnityEngine;

/// <summary>
/// Infection Blob Boss — main controller.
///
/// Expected GameObject hierarchy:
///   BossRoot          — Rigidbody2D, InfectionBlobBoss, BossHealthBridge
///   ├── BossSprite    — SpriteRenderer, Animator
///   ├── GroundCheck   — GroundCheck, Collider2D
///   ├── AttackHitbox  — Damage, Collider2D (trigger, enabled only during attacks)
///   └── BodyHitbox    — Health, Collider2D (trigger, always on)
///
/// Unity 2022.3 LTS compatible.
/// </summary>
public class InfectionBlobBoss : Boss
{
    // ---------------------------------------------------------------
    //  State Machine
    // ---------------------------------------------------------------
    private enum BossState { Idle, Moving, DirectionalAttack, BurstAttack, Dead }
    private BossState currentState = BossState.Idle;

    // ---------------------------------------------------------------
    //  Child references — assign in Inspector
    // ---------------------------------------------------------------
    [Header("Child References")]
    [Tooltip("Child with SpriteRenderer and Animator.")]
    [SerializeField] private GameObject bossSprite;

    [Tooltip("Child with GroundCheck component.")]
    [SerializeField] private GameObject groundCheckObject;

    [Tooltip("Child with Damage component — enabled only during attacks.")]
    [SerializeField] private GameObject attackHitbox;

    [Tooltip("Child with Health component — always active.")]
    [SerializeField] private GameObject bodyHitbox;

    // ---------------------------------------------------------------
    //  Movement
    // ---------------------------------------------------------------
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float slitherAmplitude = 0.06f;
    [SerializeField] private float slitherFrequency = 3f;
    [SerializeField] private float moveDuration = 2.5f;

    // ---------------------------------------------------------------
    //  Directional Attack
    // ---------------------------------------------------------------
    [Header("Directional Attack")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private int directionalTentacleCount = 3;
    [SerializeField] private float directionalSpread = 18f;
    [SerializeField] private float directionalAttackCooldown = 3.5f;

    // ---------------------------------------------------------------
    //  Burst Attack
    // ---------------------------------------------------------------
    [Header("Burst Attack")]
    [SerializeField] private int burstTentacleCount = 8;
    [SerializeField] private float burstAttackCooldown = 8f;
    [SerializeField] private float burstWindupTime = 1.5f;

    // ---------------------------------------------------------------
    //  Phase 2
    // ---------------------------------------------------------------
    [Header("Phase 2")]
    [SerializeField] private float phase2Threshold = 0.5f;
    [SerializeField] private float phase2SpeedMultiplier = 1.6f;
    [SerializeField] private float phase2CooldownMultiplier = 0.65f;

    // ---------------------------------------------------------------
    //  Effects
    // ---------------------------------------------------------------
    [Header("Effects")]
    [SerializeField] private GameObject burstWarningEffect;
    [SerializeField] private GameObject deathExplosionEffect;

    // ---------------------------------------------------------------
    //  Animator hashes
    // ---------------------------------------------------------------
    private static readonly int AnimCrawl       = Animator.StringToHash("Crawl");
    private static readonly int AnimAttackDir   = Animator.StringToHash("AttackDir");
    private static readonly int AnimAttackBurst = Animator.StringToHash("AttackBurst");
    private static readonly int AnimDeath       = Animator.StringToHash("Death");

    // ---------------------------------------------------------------
    //  Cached component references
    // ---------------------------------------------------------------
    private Transform      playerTransform;
    private Rigidbody2D    rb;
    private Health         health;
    private SpriteRenderer spriteRenderer;
    private Animator       animator;
    private GroundCheck    groundCheck;
    private Collider2D     attackHitboxCollider;
    private Damage         attackHitboxDamage;

    private float nextDirectionalAttackTime;
    private float nextBurstAttackTime;
    private float moveTimer;
    private int   moveDirection = 1;
    private bool  isPhase2      = false;
    private float slitherTime   = 0f;

    // ---------------------------------------------------------------
    //  Initialisation
    // ---------------------------------------------------------------
    public override void Initialize(ArenaController arenaController)
    {
        base.Initialize(arenaController);

        // Root
        rb = GetComponent<Rigidbody2D>();

        // BossSprite child
        if (bossSprite != null)
        {
            spriteRenderer = bossSprite.GetComponent<SpriteRenderer>();
            animator       = bossSprite.GetComponent<Animator>();
        }
        else Debug.LogWarning("InfectionBlobBoss: bossSprite not assigned.", this);

        // GroundCheck child
        if (groundCheckObject != null)
            groundCheck = groundCheckObject.GetComponent<GroundCheck>();
        else Debug.LogWarning("InfectionBlobBoss: groundCheckObject not assigned.", this);

        // AttackHitbox child — disabled until attack fires
        if (attackHitbox != null)
        {
            attackHitboxCollider = attackHitbox.GetComponent<Collider2D>();
            attackHitboxDamage   = attackHitbox.GetComponent<Damage>();
            SetAttackHitbox(false);
        }
        else Debug.LogWarning("InfectionBlobBoss: attackHitbox not assigned.", this);

        // BodyHitbox child
        if (bodyHitbox != null)
            health = bodyHitbox.GetComponent<Health>();
        else Debug.LogWarning("InfectionBlobBoss: bodyHitbox not assigned.", this);

        nextDirectionalAttackTime = Time.time + 2f;
        nextBurstAttackTime       = Time.time + burstAttackCooldown;

        PickNewMoveDirection();
        StartCoroutine(BossLoop());
    }

    /// <summary>Called by EnemySpawner after spawning so the boss knows where the player is.</summary>
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    // ---------------------------------------------------------------
    //  Boss death — called by BossHealthBridge
    // ---------------------------------------------------------------
    public override void OnBossDeath()
    {
        if (currentState == BossState.Dead) return;
        currentState = BossState.Dead;

        StopAllCoroutines();
        SetAttackHitbox(false);
        SetAnimTrigger(AnimDeath);

        if (deathExplosionEffect != null)
            Instantiate(deathExplosionEffect, transform.position, Quaternion.identity);

        arena.ForceEndArena();
        Destroy(gameObject, 1.5f);
    }

    // ---------------------------------------------------------------
    //  Main loop
    // ---------------------------------------------------------------
    private IEnumerator BossLoop()
    {
        while (currentState != BossState.Dead)
        {
            CheckPhaseTransition();

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
                if (currentState != BossState.Moving)
                {
                    currentState = BossState.Moving;
                    SetAnimTrigger(AnimCrawl);
                }
                yield return null;
            }
        }
    }

    // ---------------------------------------------------------------
    //  Physics
    // ---------------------------------------------------------------
    private void FixedUpdate()
    {
        if (currentState != BossState.Moving) return;
        SlitherAlongGround();
    }

    private void SlitherAlongGround()
    {
        float speed = moveSpeed * (isPhase2 ? phase2SpeedMultiplier : 1f);

        Vector2 velocity = rb.velocity;
        velocity.x = moveDirection * speed;

        // Slow horizontal movement while airborne so the boss doesn't slide off edges
        bool grounded = groundCheck != null && groundCheck.CheckGrounded();
        if (!grounded)
            velocity.x *= 0.4f;

        rb.velocity = velocity;

        // Sinusoidal body bob
        slitherTime += Time.fixedDeltaTime * slitherFrequency;
        Vector3 pos = transform.position;
        pos.y += Mathf.Sin(slitherTime) * slitherAmplitude;
        transform.position = pos;

        // Flip sprite to face movement direction
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveDirection < 0;

        moveTimer -= Time.fixedDeltaTime;
        if (moveTimer <= 0f)
            PickNewMoveDirection();
    }

    private void PickNewMoveDirection()
    {
        moveDirection = (playerTransform != null)
            ? (playerTransform.position.x >= transform.position.x ? 1 : -1)
            : (Random.value > 0.5f ? 1 : -1);

        moveTimer = moveDuration;
    }

    // ---------------------------------------------------------------
    //  Directional Attack
    // ---------------------------------------------------------------
    private IEnumerator DoDirectionalAttack()
    {
        currentState = BossState.DirectionalAttack;
        SetAnimTrigger(AnimAttackDir);

        // Wait for the animation windup frames before releasing
        // At 8fps: 6 windup frames = 0.75s. Tune to match your animation.
        yield return new WaitForSeconds(0.75f);

        SetAttackHitbox(true);

        if (playerTransform != null && tentaclePrefab != null)
        {
            Vector2 toPlayer  = (playerTransform.position - transform.position).normalized;
            float   baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float   startOffset = -(directionalTentacleCount - 1) * 0.5f * directionalSpread;

            for (int i = 0; i < directionalTentacleCount; i++)
                SpawnTentacle(AngleToDir(baseAngle + startOffset + i * directionalSpread));
        }

        // Brief window where the body hitbox is also dangerous to touch
        yield return new WaitForSeconds(0.125f);
        SetAttackHitbox(false);

        float cooldown = directionalAttackCooldown * (isPhase2 ? phase2CooldownMultiplier : 1f);
        nextDirectionalAttackTime = Time.time + cooldown;

        // Wait for recovery frames before returning to move state
        yield return new WaitForSeconds(0.5f);
    }

    // ---------------------------------------------------------------
    //  Burst Attack
    // ---------------------------------------------------------------
    private IEnumerator DoBurstAttack()
    {
        currentState = BossState.BurstAttack;
        SetAnimTrigger(AnimAttackBurst);

        if (burstWarningEffect != null)
        {
            GameObject warning = Instantiate(burstWarningEffect,
                transform.position, Quaternion.identity, transform);
            Destroy(warning, burstWindupTime);
        }

        yield return new WaitForSeconds(burstWindupTime);

        SetAttackHitbox(true);

        if (tentaclePrefab != null)
        {
            float step = 360f / burstTentacleCount;
            for (int i = 0; i < burstTentacleCount; i++)
                SpawnTentacle(AngleToDir(i * step));
        }

        yield return new WaitForSeconds(0.125f);
        SetAttackHitbox(false);

        float cooldown = burstAttackCooldown * (isPhase2 ? phase2CooldownMultiplier : 1f);
        nextBurstAttackTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.625f);
    }

    // ---------------------------------------------------------------
    //  Phase 2
    // ---------------------------------------------------------------
    private void CheckPhaseTransition()
    {
        if (isPhase2 || health == null) return;
        if ((float)health.currentHealth / health.maximumHealth <= phase2Threshold)
        {
            isPhase2 = true;
            OnEnterPhase2();
        }
    }

    private void OnEnterPhase2()
    {
        nextBurstAttackTime       = Time.time + 1f;
        nextDirectionalAttackTime = Time.time + 2f;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.9f, 0.4f, 1f);
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------
    private void SpawnTentacle(Vector2 direction)
    {
        GameObject obj = Instantiate(tentaclePrefab, transform.position, Quaternion.identity);
        BossTentacle t = obj.GetComponent<BossTentacle>();
        if (t != null) t.Launch(direction);
    }

    /// <summary>
    /// Toggles the attack hitbox collider and Damage component on/off.
    /// The Damage component on AttackHitbox should have:
    ///   - teamId = 1  (boss team)
    ///   - dealDamageOnTriggerEnter = true
    ///   - destroyAfterDamage = false
    /// </summary>
    private void SetAttackHitbox(bool active)
    {
        if (attackHitboxCollider != null) attackHitboxCollider.enabled = active;
        if (attackHitboxDamage   != null) attackHitboxDamage.enabled   = active;
    }

    private void SetAnimTrigger(int hash)
    {
        if (animator != null) animator.SetTrigger(hash);
    }

    private static Vector2 AngleToDir(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}