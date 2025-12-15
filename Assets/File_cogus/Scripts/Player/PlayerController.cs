using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerController : MonoBehaviour
{
    public static PlayerController Instance
    {
        get;
        private set;
    }

    public event Action<PlayerSkin> OnSkinChanged;
    public event Action OnPausePressed;
    public event Action OnDied;

    [Header("Scene Refs")]
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerSettings settings;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D groundCheckBox;

    [Header("Start Skin")]
    [SerializeField] private PlayerSkin startSkin = PlayerSkin.Egg;

    [Header("Prefabs")]
    [SerializeField] private EggProjectile eggProjectilePrefab;
    [SerializeField] private BananaPeel bananaPeelPrefab;
    [SerializeField] private HealingBanana healingBananaPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform eggFirePoint;
    [SerializeField] private Transform bananaThrowPoint;
    [SerializeField] private Transform healingBananaDropPoint;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;

    private PlayerStateMachine stateMachine;

    public PlayerVitals Vitals => vitals;
    public PlayerSettings Settings => settings;

    public PlayerSkin CurrentSkin
    {
        get;
        private set;
    }

    public float MoveInput
    {
        get;
        private set;
    }

    public bool JumpHeld
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesDown
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesUp
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesHeld
    {
        get;
        private set;
    }

    public bool HealingBananaThrowDown
    {
        get;
        private set;
    }

    public bool IsGround
    {
        get;
        private set;
    }

    public int FacingDirection
    {
        get;
        private set;
    } = 1;

    public bool IsJumping
    {
        get;
        private set;
    }

    public bool IsSnailHidden
    {
        get;
        private set;
    }

    public Vector2 CurrentVelocity => rb.linearVelocity;

    public bool HasJumpBuffer => jumpBufferTimer > 0f;

    public bool HasCoyote
    {
        get { return coyoteTimer > 0f; }
    }

    private float currentSpeedAbs;
    private int lastMoveSign;
    private float jumpTimeCounter;
    private float jumpBufferTimer;
    private float coyoteTimer;

    private bool dead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CurrentSkin = startSkin;

        stateMachine = new PlayerStateMachine();
        stateMachine.Initialize(CreateStateForSkin(CurrentSkin));

        OnSkinChanged?.Invoke(CurrentSkin);
    }

    private void Update()
    {
        if (dead) return;

        PollInput();
        UpdateGround();

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (coyoteTimer > 0f && !IsGround)
            coyoteTimer -= Time.deltaTime;

        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        if (dead) return;

        stateMachine.FixedUpdate();
    }

    private void PollInput()
    {
        InputService input = InputService.Instance;
        if (input == null) return;

        MoveInput = input.MoveAxis;

        SpecialAbilitiesDown = input.SpecialAbilitiesDown;
        SpecialAbilitiesUp = input.SpecialAbilitiesUp;
        SpecialAbilitiesHeld = input.SpecialAbilitiesHeld;

        HealingBananaThrowDown = input.HealingBananaThrowDown;

        if (input.SkinChangeLeftDown)
            TrySwitchSkin(-1);

        if (input.SkinChangeRightDown)
            TrySwitchSkin(1);

        if (input.PauseDown)
            OnPausePressed?.Invoke();

        bool canJump = CurrentSkin == PlayerSkin.Banana && !IsSnailHidden;

        if (canJump)
        {
            if (input.JumpDown && settings != null)
                jumpBufferTimer = settings.jumpBufferTime;

            JumpHeld = input.JumpHeld;

            if (input.JumpUp)
                StopRising();
        }
        else
        {
            JumpHeld = false;
            jumpBufferTimer = 0f;

            if (IsJumping)
                CancelJump();
        }
    }

    private void UpdateGround()
    {
        IsGround = groundCheckBox != null && groundCheckBox.IsTouchingLayers(groundLayer);

        if (IsGround && settings != null)
            coyoteTimer = settings.coyoteTime;
    }

    private PlayerState CreateStateForSkin(PlayerSkin skin)
    {
        switch (skin)
        {
            case PlayerSkin.Egg:
                return new EggState(this, stateMachine);
            case PlayerSkin.Banana:
                return new BananaState(this, stateMachine);
            case PlayerSkin.Snail:
                return new SnailState(this, stateMachine);
        }

        return new EggState(this, stateMachine);
    }

    private bool TrySwitchSkin(int delta)
    {
        if (vitals == null) return false;

        int count = Enum.GetValues(typeof(PlayerSkin)).Length;
        int index = (int)CurrentSkin;

        for (int i = 0; i < count; i++)
        {
            index = (index + delta) % count;
            if (index < 0) index += count;

            PlayerSkin next = (PlayerSkin)index;

            if (vitals.GetHealth(next) > 0)
            {
                SwitchSkin(next);
                return true;
            }
        }

        return false;
    }

    private void SwitchSkin(PlayerSkin next)
    {
        if (next == CurrentSkin) return;

        SetSnailHidden(false);
        CancelJump();
        ConsumeJumpBuffer();

        CurrentSkin = next;
        stateMachine.ChangeState(CreateStateForSkin(CurrentSkin));

        currentSpeedAbs = 0f;
        lastMoveSign = 0;

        OnSkinChanged?.Invoke(CurrentSkin);
    }

    public void ConsumeJumpBuffer()
    {
        jumpBufferTimer = 0f;
    }

    public void StartJump()
    {
        ConsumeJumpBuffer();
        IsJumping = true;
        jumpTimeCounter = 0f;
    }

    public void CancelJump()
    {
        IsJumping = false;
        StopRising();
    }

    public void HandleMove(float speed)
    {
        int inputSign = 0;

        if (MoveInput > 0.01f)
            inputSign = 1;
        else if (MoveInput < -0.01f)
            inputSign = -1;

        float dt = Time.fixedDeltaTime;

        if (inputSign == 0)
        {
            if (IsGround)
            {
                currentSpeedAbs = 0f;
            }
            else
            {
                float baseSpeed = Mathf.Abs(rb.linearVelocity.x);

                if (settings != null && settings.airReleaseDecelTime > 0f)
                {
                    float decel = (baseSpeed / settings.airReleaseDecelTime) * dt;
                    currentSpeedAbs = Mathf.Max(0f, baseSpeed - decel);
                }
                else
                {
                    currentSpeedAbs = 0f;
                }

                if (Mathf.Abs(rb.linearVelocity.x) > 0.001f)
                    lastMoveSign = rb.linearVelocity.x >= 0f ? 1 : -1;
                else if (currentSpeedAbs <= 0.001f)
                    lastMoveSign = 0;
            }
        }
        else
        {
            bool directionChanged = lastMoveSign != 0 && inputSign != lastMoveSign && currentSpeedAbs > 0.001f;

            float startSpeed = settings != null ? Mathf.Max(0.0001f, settings.startSpeedRatio * speed) : speed;
            if (directionChanged || currentSpeedAbs <= 0f)
                currentSpeedAbs = startSpeed;

            float accelTime = settings != null ? (IsGround ? settings.groundAccelTime : settings.airAccelTime) : 0f;

            if (accelTime <= 0f)
            {
                currentSpeedAbs = speed;
            }
            else
            {
                float accel = (speed - currentSpeedAbs) / accelTime * dt;
                currentSpeedAbs = Mathf.Clamp(currentSpeedAbs + accel, 0f, speed);
            }

            lastMoveSign = inputSign;
            FacingDirection = inputSign > 0 ? 1 : -1;
        }

        float vxDir;

        if (inputSign != 0)
            vxDir = inputSign;
        else if (currentSpeedAbs > 0.001f)
            vxDir = lastMoveSign;
        else
            vxDir = 0f;

        float vx = vxDir * currentSpeedAbs;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);
    }

    public void HandleJump()
    {
        if (!IsJumping) return;
        if (settings == null) return;

        jumpTimeCounter += Time.fixedDeltaTime;

        float t = settings.maxJumpTime <= 0f ? 1f : jumpTimeCounter / settings.maxJumpTime;
        float curve = settings.jumpForceCurve != null ? settings.jumpForceCurve.Evaluate(t) : 1f;
        float force = curve * settings.maxJumpForce;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);

        if (jumpTimeCounter >= settings.maxJumpTime)
            IsJumping = false;
    }

    public void StopRising()
    {
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.3f);

        if (settings != null)
            jumpTimeCounter = settings.maxJumpTime;
    }

    public void StopAllMotion()
    {
        rb.linearVelocity = Vector2.zero;
        currentSpeedAbs = 0f;
        lastMoveSign = 0;
    }

    public void SetSnailHidden(bool hidden)
    {
        if (IsSnailHidden == hidden) return;

        IsSnailHidden = hidden;

        if (vitals != null)
            vitals.SetForcedInvincible(hidden);

        if (hidden)
            StopAllMotion();
    }

    public void FireEggProjectile()
    {
        if (eggProjectilePrefab == null) return;
        if (settings == null) return;

        Vector3 pos = eggFirePoint != null ? eggFirePoint.position : transform.position;

        EggProjectile p = Instantiate(eggProjectilePrefab, pos, Quaternion.identity);
        p.Initialize(gameObject, FacingDirection, settings.eggProjectileSpeed, settings.eggProjectileMaxDistance, settings.GetEggDamage(PlayerSkin.Egg));
    }

    public void ThrowBananaPeel()
    {
        if (bananaPeelPrefab == null) return;
        if (settings == null) return;

        Vector3 pos = bananaThrowPoint != null ? bananaThrowPoint.position : transform.position;

        BananaPeel p = Instantiate(bananaPeelPrefab, pos, Quaternion.identity);
        p.Initialize(gameObject, FacingDirection, settings.bananaPeelSpeed, settings.bananaPeelLinearDrag, settings.bananaPeelStopSpeed, settings.bananaStunDuration);
    }

    public void DropHealingBanana()
    {
        if (healingBananaPrefab == null) return;
        if (settings == null) return;

        Vector3 pos = healingBananaDropPoint != null ? healingBananaDropPoint.position : transform.position;

        HealingBanana b = Instantiate(healingBananaPrefab, pos, Quaternion.identity);
        b.Initialize(settings.healingBananaActivateDelay, settings.healingBananaHealAmount);
    }

    public bool TryHit(int damage, Vector2 attackPos)
    {
        if (vitals == null) return false;

        if (!vitals.ApplyDamage(CurrentSkin, damage, false))
            return false;

        if (vitals.GetHealth(CurrentSkin) > 0)
            return true;

        if (vitals.HasAnyAliveSkin())
        {
            TrySwitchSkin(1);
            return true;
        }

        Die();
        return true;
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        StopAllMotion();
        rb.simulated = false;
        boxCol.enabled = false;

        OnDied?.Invoke();
    }

    public Rigidbody2D Rigidbody => rb;
    public BoxCollider2D BoxCollider => boxCol;
}
