using System.Collections;
using UnityEngine;

public class BlobBoss : Boss
{
    //  State Machine
    private enum BossState { Idle, Moving, DirectionalAttack, BurstAttack, Dead }
    private BossState currentState = BossState.Idle;

    //  Child references — assign in Inspector
    [Header("Child References")]
    [SerializeField] private GameObject bossSprite;
    [SerializeField] private GameObject groundCheckObject;
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private GameObject bodyHitbox;

    //  Movement
    [Header("Movement")]
    [SerializeField] private float moveSpeed        = 1.8f;
    [SerializeField] private float slitherAmplitude = 0.06f;
    [SerializeField] private float slitherFrequency = 3f;
    [SerializeField] private float moveDuration     = 2.5f;

    //  Directional Attack
    [Header("Directional Attack")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private int   directionalTentacleCount = 3;
    [SerializeField] private float directionalSpread        = 18f;
    [SerializeField] private float directionalAttackCooldown = 3.5f;

    //  Burst Attack
    [Header("Burst Attack")]
    [SerializeField] private int   burstTentacleCount  = 8;
    [SerializeField] private float burstAttackCooldown = 8f;
    [SerializeField] private float burstWindupTime     = 1.5f;

    //  Attack Range
    [Header("Attack Range")]
    [SerializeField] private float directionalAttackRange = 6f;
    [SerializeField] private float burstAttackRange       = 4f;
    [SerializeField] private float tentacleSpawnRadius    = 1.8f;

    //  Phase 2
    [Header("Phase 2")]
    [SerializeField] private float phase2Threshold          = 0.5f;
    [SerializeField] private float phase2SpeedMultiplier    = 1.6f;
    [SerializeField] private float phase2CooldownMultiplier = 0.65f;

    //  Effects
    [Header("Effects")]
    [SerializeField] private GameObject burstWarningEffect;
    [SerializeField] private GameObject deathExplosionEffect;

    //  Animator hashes
    private static readonly int AnimCrawl       = Animator.StringToHash("Crawl");
    private static readonly int AnimAttackDir   = Animator.StringToHash("AttackDir");
    private static readonly int AnimAttackBurst = Animator.StringToHash("AttackBurst");
    private static readonly int AnimDeath       = Animator.StringToHash("Death");

    //  Cached components
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

    // ─────────────────────────────────────────────────────────────────────────
    public override void Initialize(ArenaController arenaController, Transform player)
    {
        base.Initialize(arenaController, player);

        rb = GetComponent<Rigidbody2D>();

        if (bossSprite != null)
        {
            spriteRenderer = bossSprite.GetComponent<SpriteRenderer>();
            animator       = bossSprite.GetComponent<Animator>();
        }
        else Debug.LogWarning("BlobBoss: bossSprite not assigned.", this);

        if (groundCheckObject != null)
            groundCheck = groundCheckObject.GetComponent<GroundCheck>();
        else Debug.LogWarning("BlobBoss: groundCheckObject not assigned.", this);

        if (attackHitbox != null)
        {
            attackHitboxCollider = attackHitbox.GetComponent<Collider2D>();
            attackHitboxDamage   = attackHitbox.GetComponent<Damage>();
            SetAttackHitbox(false);
        }
        else Debug.LogWarning("BlobBoss: attackHitbox not assigned.", this);

        if (bodyHitbox != null)
            health = bodyHitbox.GetComponent<Health>();
        else Debug.LogWarning("BlobBoss: bodyHitbox not assigned.", this);

        nextDirectionalAttackTime = Time.time + 2f;
        nextBurstAttackTime       = Time.time + burstAttackCooldown;

        PickNewMoveDirection();
        StartCoroutine(BossLoop());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Boss death — called by BossHealthBridge
    // ─────────────────────────────────────────────────────────────────────────
    public override void OnBossDeath()
    {
        if (currentState == BossState.Dead) return;
        currentState = BossState.Dead;

        StopAllCoroutines();
        SetAttackHitbox(false);
        SetAnimTrigger(AnimDeath);

        if (deathExplosionEffect != null)
            Instantiate(deathExplosionEffect, transform.position, Quaternion.identity);

        // RegisterKill(true) routes through ArenaController's boss path:
        // → NotifyLevelCleared() → RunManager.OnBossCleared()
        // → VictoryUIController.ShowVictory()
        arena.RegisterKill(true);

        Destroy(gameObject, 1.5f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Main loop
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator BossLoop()
    {
        while (currentState != BossState.Dead)
        {
            CheckPhaseTransition();

            float distToPlayer = playerTransform != null
                ? Vector2.Distance(transform.position, playerTransform.position)
                : float.MaxValue;

            if (Time.time >= nextBurstAttackTime && distToPlayer <= burstAttackRange)
            {
                yield return StartCoroutine(DoBurstAttack());
            }
            else if (Time.time >= nextDirectionalAttackTime
                     && playerTransform != null
                     && distToPlayer <= directionalAttackRange)
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

    // ─────────────────────────────────────────────────────────────────────────
    // Physics
    // ─────────────────────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (currentState != BossState.Moving) return;
        SlitherAlongGround();
    }

    private void SlitherAlongGround()
    {
        float speed    = moveSpeed * (isPhase2 ? phase2SpeedMultiplier : 1f);
        Vector2 velocity = rb.velocity;
        velocity.x = moveDirection * speed;

        bool grounded = groundCheck != null && groundCheck.CheckGrounded();
        if (!grounded) velocity.x *= 0.4f;
        rb.velocity = velocity;

        slitherTime += Time.fixedDeltaTime * slitherFrequency;
        Vector3 pos = transform.position;
        pos.y += Mathf.Sin(slitherTime) * slitherAmplitude;
        transform.position = pos;

        if (spriteRenderer != null)
            spriteRenderer.flipX = moveDirection < 0;

        moveTimer -= Time.fixedDeltaTime;
        if (moveTimer <= 0f) PickNewMoveDirection();
    }

    private void PickNewMoveDirection()
    {
        moveDirection = (playerTransform != null)
            ? (playerTransform.position.x >= transform.position.x ? 1 : -1)
            : (Random.value > 0.5f ? 1 : -1);
        moveTimer = moveDuration;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attacks
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoDirectionalAttack()
    {
        currentState = BossState.DirectionalAttack;
        SetAnimTrigger(AnimAttackDir);
        yield return new WaitForSeconds(0.75f);
        SetAttackHitbox(true);

        if (playerTransform != null && tentaclePrefab != null)
        {
            Vector2 toPlayer   = (playerTransform.position - transform.position).normalized;
            float   baseAngle  = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float   startOffset = -(directionalTentacleCount - 1) * 0.5f * directionalSpread;
            for (int i = 0; i < directionalTentacleCount; i++)
                SpawnTentacle(AngleToDir(baseAngle + startOffset + i * directionalSpread));
        }

        yield return new WaitForSeconds(0.125f);
        SetAttackHitbox(false);

        float cooldown = directionalAttackCooldown * (isPhase2 ? phase2CooldownMultiplier : 1f);
        nextDirectionalAttackTime = Time.time + cooldown;
        yield return new WaitForSeconds(0.5f);
    }

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

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2
    // ─────────────────────────────────────────────────────────────────────────
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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.9f, 0.4f, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnTentacle(Vector2 direction)
    {
        Vector3 spawnPos = transform.position + (Vector3)(direction.normalized * tentacleSpawnRadius);
        GameObject obj   = Instantiate(tentaclePrefab, spawnPos, Quaternion.identity);
        BossTentacle t   = obj.GetComponent<BossTentacle>();
        if (t != null)
        {
            if (spriteRenderer != null) t.SetColor(spriteRenderer.color);
            t.Launch(direction);
        }
    }

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