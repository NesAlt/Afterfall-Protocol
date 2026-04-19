using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Game Object and Component References")]
    public GroundCheck groundCheck = null;
    public SpriteRenderer spriteRenderer = null;
    public Health playerHealth;

    [SerializeField] private Transform firePoint;
    private Vector3 firePointRightLocalPos;
    private Vector3 firePointLeftLocalPos;
    private Rigidbody2D playerRigidbody = null;

    #region Directional Facing
    public enum PlayerDirection { Right, Left }

    public PlayerDirection facing
    {
        get
        {
            if (moveAction.ReadValue<Vector2>().x > 0)       return PlayerDirection.Right;
            else if (moveAction.ReadValue<Vector2>().x < 0)  return PlayerDirection.Left;
            else
            {
                if (spriteRenderer != null && spriteRenderer.flipX) return PlayerDirection.Left;
                return PlayerDirection.Right;
            }
        }
    }
    #endregion

    public bool grounded => groundCheck != null && groundCheck.CheckGrounded();

    [Header("Movement Settings")]
    [Tooltip("Base movement speed — buff multiplier is applied on top at runtime.")]
    public float movementSpeed = 4.0f;
    private float _effectiveSpeed;   // movementSpeed × (1 + MoveSpeedBonus)

    [Header("Jump Settings")]
    public float jumpPower    = 10.0f;
    public int   allowedJumps = 1;
    public float jumpDuration = 0.1f;
    public GameObject jumpEffect = null;

    [Header("Combat")]
    public bool isAttacking = false;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Base seconds between shots — reduced by FireRate buff.")]
    [SerializeField] private float baseFireCooldown = 0.5f;
    private float _fireCooldown;        // baseFireCooldown / (1 + FireRateBonus)
    private float _lastFireTime = -999f;

    [Tooltip("Base bullets fired per shot — increased by BulletCount buff.")]
    [SerializeField] private int baseBulletCount = 1;


    private int _effectiveBulletCount;  // baseBulletCount + BulletCountBonus

    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private LayerMask wallLayer;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;

    [Header("Input Actions & Controls")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction attackAction;

    [Header("Wall Jump")]
    [SerializeField] private float maxFallSpeed = 20f;
    public float wallJumpForce = 10f;
    public Vector2 wallJumpDirection = new Vector2(1, 1);
    private float wallJumpLockTime    = 0.2f;
    private float wallJumpLockCounter;

    [SerializeField] private float buildingJumpMultiplier = 1.5f;
    private bool isInsideBuilding = false;

    private int  timesJumped = 0;
    private bool jumping     = false;

    public enum PlayerState { Idle, Walk, Jump, Fall, Dead }
    public PlayerState state = PlayerState.Idle;


    void OnEnable()  { moveAction.Enable();  jumpAction.Enable();  attackAction.Enable(); }
    void OnDisable() { moveAction.Disable(); jumpAction.Disable(); attackAction.Disable(); }

    private void Start()
    {
        SetupRigidbody();

        if (firePoint != null)
        {
            firePointRightLocalPos = firePoint.localPosition;
            firePointLeftLocalPos  = new Vector3(
                -firePointRightLocalPos.x,
                 firePointRightLocalPos.y,
                 firePointRightLocalPos.z);
        }

        ApplyBuffs();
    }

    private void ApplyBuffs()
    {
        if (PlayerBuffManager.Instance != null)
        {
            // Movement speed
            _effectiveSpeed = movementSpeed * (1f + PlayerBuffManager.Instance.MoveSpeedBonus);

            // Fire rate — higher bonus = shorter cooldown
            _fireCooldown = baseFireCooldown / (1f + PlayerBuffManager.Instance.FireRateBonus);

            // Bullet count
            _effectiveBulletCount = baseBulletCount + PlayerBuffManager.Instance.BulletCountBonus;
        }
        else
        {
            // No buff manager (editor testing) — use base values
            _effectiveSpeed       = movementSpeed;
            _fireCooldown         = baseFireCooldown;
            _effectiveBulletCount = baseBulletCount;
        }

        Debug.Log($"[Player] Speed: {_effectiveSpeed:F2} | FireCooldown: {_fireCooldown:F2}s | Bullets: {_effectiveBulletCount}");
    }


    private void FixedUpdate()
    {
        if (playerRigidbody.velocity.y < -maxFallSpeed)
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, -maxFallSpeed);
    }

    private void LateUpdate()
    {
        if (wallJumpLockCounter > 0) wallJumpLockCounter -= Time.deltaTime;

        isTouchingWallLeft  = Physics2D.OverlapCircle(wallCheckLeft.position,  wallCheckRadius, wallLayer);
        isTouchingWallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        ProcessInput();
        HandleSpriteDirection();
        DetermineState();
    }


    private void ProcessInput()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleAttackInput();
    }

    private void HandleMovementInput()
    {
        if (isAttacking || state == PlayerState.Dead) { MovePlayer(Vector2.zero); return; }

        Vector2 movementForce = Vector2.zero;
        if (Mathf.Abs(moveAction.ReadValue<Vector2>().x) > 0)
            movementForce = transform.right * _effectiveSpeed * moveAction.ReadValue<Vector2>().x;

        MovePlayer(movementForce);
    }

    private void HandleAttackInput()
    {
        if (!attackAction.triggered || isAttacking || !grounded || state == PlayerState.Dead)
            return;
        if (Time.time < _lastFireTime + _fireCooldown)
            return;
        StartCoroutine(AttackRoutine());
    }


    [Tooltip("Delay in seconds between each bullet in a burst.")]
    [SerializeField] private float burstDelay = 0.08f;

    public void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        _lastFireTime = Time.time;
        StartCoroutine(BurstFireRoutine());
    }

    private IEnumerator BurstFireRoutine()
    {
        float xDir  = Mathf.Sign(firePoint.localPosition.x);
        Vector2 dir = new Vector2(xDir, 0f);
        int count   = Mathf.Max(1, _effectiveBulletCount);

        for (int i = 0; i < count; i++)
        {
            GameObject proj   = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile bullet = proj.GetComponent<Projectile>();
            if (bullet != null) bullet.Init(dir);

            if (i < count - 1)
                yield return new WaitForSeconds(burstDelay);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        playerRigidbody.velocity = new Vector2(0, playerRigidbody.velocity.y);

        PlayerAnimator animator = GetComponent<PlayerAnimator>();
        if (animator != null) animator.PlayAttack();

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    private void MovePlayer(Vector2 movementForce)
    {
        // If attacking, kill horizontal velocity and do nothing else
        if (isAttacking)
        {
            playerRigidbody.velocity = new Vector2(0, playerRigidbody.velocity.y);
            return;
        }

        float inputX = moveAction.ReadValue<Vector2>().x;

        if ((isTouchingWallLeft || isTouchingWallRight) &&
            !grounded &&
            playerRigidbody.velocity.y <= 0 &&
            wallJumpLockCounter <= 0)
        {
            float horizontal = inputX * _effectiveSpeed;
            if (isTouchingWallLeft  && inputX < 0) horizontal = 0;  
            if (isTouchingWallRight && inputX > 0) horizontal = 0;
            playerRigidbody.velocity = new Vector2(horizontal, -2f);
        }
        else
        {
            playerRigidbody.velocity = new Vector2(inputX * _effectiveSpeed, playerRigidbody.velocity.y);
        }
    }

    private void HandleJumpInput()
    {
        if (isAttacking || state == PlayerState.Dead) return;

        if (jumpAction.triggered)
        {
            if (isTouchingWallLeft || isTouchingWallRight) { WallJump(); return; }
            float multiplier = isInsideBuilding ? buildingJumpMultiplier : 1.0f;
            StartCoroutine("Jump", multiplier);
        }
    }

    private void WallJump()
    {
        float direction = isTouchingWallLeft ? 1f : -1f;
        wallJumpLockCounter = wallJumpLockTime;
        GetComponent<FallDamage>().NotifyWallJump();
        playerRigidbody.velocity = Vector2.zero;
        playerRigidbody.AddForce(new Vector2(direction * wallJumpDirection.x, wallJumpDirection.y) * wallJumpForce, ForceMode2D.Impulse);
    }

    private IEnumerator Jump(float powerMultiplier = 1.0f)
    {
        if (timesJumped < allowedJumps && state != PlayerState.Dead)
        {
            jumping = true;
            float time = 0;
            SpawnJumpEffect();
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0);
            playerRigidbody.AddForce(transform.up * jumpPower * powerMultiplier, ForceMode2D.Impulse);
            timesJumped++;
            while (time < jumpDuration) { yield return null; time += Time.deltaTime; }
            jumping = false;
        }
    }

    private void SpawnJumpEffect()
    {
        if (jumpEffect != null) Instantiate(jumpEffect, transform.position, transform.rotation, null);
    }

    public void Bounce()
    {
        timesJumped = 0;
        StartCoroutine("Jump", jumpAction.ReadValue<float>() >= 1 ? 1.5f : 1.0f);
    }

    public void SetInsideBuilding(bool value) { if (!grounded) return; isInsideBuilding = value; }

    private void HandleSpriteDirection()
    {
        if (spriteRenderer == null) return;
        bool left = facing == PlayerDirection.Left;
        spriteRenderer.flipX = left;
        if (firePoint != null)
            firePoint.localPosition = left ? firePointLeftLocalPos : firePointRightLocalPos;
    }

    private void DetermineState()
    {
        if (isAttacking) return;

        if (playerHealth.currentHealth <= 0)
        {
            state = PlayerState.Dead;
        }
        else if (grounded)
        {
            state = playerRigidbody.velocity.magnitude > 0 ? PlayerState.Walk : PlayerState.Idle;
            if (!jumping) timesJumped = 0;
        }
        else
        {
            state = jumping ? PlayerState.Jump : PlayerState.Fall;
        }
    }

    private void SetupRigidbody()
    {
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();
    }
}