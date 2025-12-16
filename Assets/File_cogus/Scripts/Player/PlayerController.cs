using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public Animator Anim { get; private set; }

    public event Action<PlayerSkin> OnSkinChanged;
    public event Action OnPausePressed;
    public event Action OnDied;

    public const string AnimEggIdle = "계란가만히있는모션";
    public const string AnimEggWalk = "계란걷는거";
    public const string AnimEggThrow = "계란이계란을던지는";

    public const string AnimBananaIdle = "바나나가만히있는모션";
    public const string AnimBananaWalk = "바나나걷는거";
    public const string AnimBananaJump = "바나나점프";

    public const string AnimSnailIdle = "달팽이가만히있는모션";
    public const string AnimSnailWalk = "달팽이걷는거";
    public const string AnimSnailEnterShell = "달팽이집들어가기";
    public const string AnimSnailExitShell = "달팽이집나오기";

    [SerializeField] private bool resetAllStatusOnAwake = false;

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

    public bool UpHeld
    {
        get;
        private set;
    }

    public bool DownHeld
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

    public bool IsEggShootLocked
    {
        get { return eggShootLockTimer > 0f; }
    }

    public bool IsMovementLocked
    {
        get { return eggShootLockTimer > 0f || movementLockTimer > 0f || IsSnailHidden; }
    }

    public bool IsSkinChangeLocked
    {
        get { return skinChangeLockForced || skinChangeLockTimer > 0f; }
    }
    public bool CanThrowBananaPeel
    {
        get
        {
            return activeBananaPeelCount < settings.maxBananaPeelCount;
        }
    }

    private float jumpTimeCounter;
    private float jumpBufferTimer;
    private float coyoteTimer;
    private float eggShootLockTimer;
    private float movementLockTimer;
    private float skinChangeLockTimer;
    private bool skinChangeLockForced;
    private float bananaPeelCooldownTimer;
    private float healingBananaCooldownTimer;
    private int activeBananaPeelCount;

    private bool dead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        if (Anim == null) Anim = GetComponentInChildren<Animator>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (resetAllStatusOnAwake) settings.ResetAllStatus();
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

        TickCooldowns();

        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (coyoteTimer > 0f && !IsGround) coyoteTimer -= Time.deltaTime;

        stateMachine.Update();
    }

    private void TickCooldowns()
    {
        float dt = Time.deltaTime;

        if (eggShootLockTimer > 0f) eggShootLockTimer -= dt;
        if (eggShootLockTimer < 0f) eggShootLockTimer = 0f;


        if (movementLockTimer > 0f) movementLockTimer -= dt;
        if (movementLockTimer < 0f) movementLockTimer = 0f;

        if (skinChangeLockTimer > 0f) skinChangeLockTimer -= dt;
        if (skinChangeLockTimer < 0f) skinChangeLockTimer = 0f;


        if (bananaPeelCooldownTimer > 0f) bananaPeelCooldownTimer -= dt;
        if (bananaPeelCooldownTimer < 0f) bananaPeelCooldownTimer = 0f;

        if (healingBananaCooldownTimer > 0f) healingBananaCooldownTimer -= dt;
        if (healingBananaCooldownTimer < 0f) healingBananaCooldownTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (dead) return;

        stateMachine.FixedUpdate();
    }

    private void PollInput()
    {
        InputService input = InputService.Instance;

        MoveInput = input.MoveAxis;

        UpHeld = input.UpHeld;
        DownHeld = input.DownHeld;

        SpecialAbilitiesDown = input.SpecialAbilitiesDown;
        SpecialAbilitiesUp = input.SpecialAbilitiesUp;
        SpecialAbilitiesHeld = input.SpecialAbilitiesHeld;

        HealingBananaThrowDown = input.HealingBananaThrowDown;

        if (!IsSkinChangeLocked)
        {
            if (input.SkinChangeLeftDown) TrySwitchSkin(-1);
            if (input.SkinChangeRightDown) TrySwitchSkin(1);
        }


        if (input.PauseDown) OnPausePressed?.Invoke();

        if (CurrentSkin == PlayerSkin.Banana && !IsSnailHidden)
        {
            if (input.JumpDown) jumpBufferTimer = Settings.jumpBufferTime;
            JumpHeld = input.JumpHeld;
            if (input.JumpUp) StopRising();
        }
        else
        {
            JumpHeld = false;
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
        return skin switch
        {
            PlayerSkin.Egg => new EggState(this, stateMachine),
            PlayerSkin.Banana => new BananaState(this, stateMachine),
            PlayerSkin.Snail => new SnailState(this, stateMachine),
            _ => new EggState(this, stateMachine),
        };
    }

    private bool TrySwitchSkin(int delta)
    {
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
        PlayerSkin prev = CurrentSkin;

        SetSnailHidden(false);
        CancelJump();

        CurrentSkin = next;

        if (CurrentSkin != PlayerSkin.Banana)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        stateMachine.ChangeState(CreateStateForSkin(CurrentSkin));

        Debug.Log("[PlayerSkin] " + prev + " -> " + CurrentSkin);

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
        if (IsMovementLocked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (CurrentSkin == PlayerSkin.Banana)
            HandleBananaMove(speed);
        else
            HandleSimpleMove(speed);
    }

    private void HandleSimpleMove(float speed)
    {
        int inputSign = 0;

        if (MoveInput > 0.01f) inputSign = 1;
        else if (MoveInput < -0.01f) inputSign = -1;

        if (inputSign != 0)
            FacingDirection = inputSign;

        float vx = inputSign * speed;
        Rigidbody.linearVelocity = new Vector2(vx, Rigidbody.linearVelocity.y);

        transform.rotation = Quaternion.Euler(0f, FacingDirection == 1 ? 180f : 0f, 0f);
    }

    private void HandleBananaMove(float speed)
    {
        float dt = Time.fixedDeltaTime;
        float maxSpeed = Mathf.Max(0f, speed);

        float vx = Rigidbody.linearVelocity.x;
        float targetVx = Mathf.Clamp(MoveInput, -1f, 1f) * maxSpeed;

        if (Mathf.Abs(MoveInput) > 0.01f)
        {
            float accelTime = Mathf.Max(0.0001f, Settings.bananaAccelTime);
            float accelRate = maxSpeed / accelTime;

            vx = Mathf.MoveTowards(vx, targetVx, accelRate * dt);
            FacingDirection = MoveInput > 0f ? 1 : -1;
        }
        else
        {
            float decelTime = Mathf.Max(0.0001f, Settings.bananaDecelTime);
            float decelRate = maxSpeed / decelTime;

            vx = Mathf.MoveTowards(vx, 0f, decelRate * dt);
        }

        Rigidbody.linearVelocity = new Vector2(vx, Rigidbody.linearVelocity.y);
        transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);
    }

    public void HandleJump()
    {
        if (!IsJumping) return;

        jumpTimeCounter += Time.fixedDeltaTime;

        float t = settings.maxJumpTime <= 0f ? 1f : jumpTimeCounter / settings.maxJumpTime;
        float curve = settings.jumpForceCurve.Evaluate(t);
        float force = curve * settings.maxJumpForce;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);

        if (jumpTimeCounter >= settings.maxJumpTime)
            IsJumping = false;
    }

    public void StopRising()
    {
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.3f);

        jumpTimeCounter = settings.maxJumpTime;
    }

    public void StopAllMotion() => rb.linearVelocity = Vector2.zero;

    public void LockMovement(float time)
    {
        if (time <= 0f) return;
        if (movementLockTimer < time)
            movementLockTimer = time;
    }

    public void LockSkinChange(float time)
    {
        if (time <= 0f) return;
        if (skinChangeLockTimer < time)
            skinChangeLockTimer = time;
    }

    public void SetForcedSkinChangeLock(bool locked)
    {
        skinChangeLockForced = locked;
    }

    public void SetSnailHidden(bool hidden)
    {
        IsSnailHidden = hidden;

        vitals.SetForcedInvincible(hidden);

        if (hidden)
            StopAllMotion();
    }

    public bool TryFireEggProjectile(float time)
    {
        if (eggShootLockTimer > 0f) return false;

        Anim.Play(AnimEggThrow, -1, 0f);

        float animTime = GetAnimLength(AnimEggThrow);
        if (animTime > 0f) time = animTime;

        if (time <= 0f) return false;

        LockSkinChange(time);

        StartCoroutine(FireEggProjectileByTime(time));
        return true;
    }

    public IEnumerator FireEggProjectileByTime(float time)
    {
        eggShootLockTimer = time;
        LockSkinChange(time);
        yield return new WaitForSeconds(time);

        Vector3 pos = eggFirePoint.position;

        Vector2 dir;

        if (UpHeld) dir = Vector2.up;
        else dir = new Vector2(FacingDirection, 0f);

        EggProjectile p = Instantiate(eggProjectilePrefab, pos, Quaternion.identity);
        p.Initialize(gameObject, dir, settings.eggProjectileSpeed, settings.eggProjectileMaxDistance, settings.baseEggProjectileDamage + settings.extraEggProjectileDamage);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public bool TryThrowBananaPeel()
    {
        if (bananaPeelCooldownTimer > 0f) return false;
        if (!CanThrowBananaPeel) return false;

        Vector3 pos = bananaThrowPoint.position;

        BananaPeel p = Instantiate(bananaPeelPrefab, pos, Quaternion.identity);
        p.Initialize(gameObject, FacingDirection, settings.bananaPeelSpeed, settings.bananaStunDuration);

        bananaPeelCooldownTimer = Mathf.Max(0f, settings.bananaPeelCooldown);
        activeBananaPeelCount++;

        return true;
    }

    public bool TryDropHealingBanana()
    {
        if (healingBananaCooldownTimer > 0f) return false;

        Vector3 pos = healingBananaDropPoint.position;

        HealingBanana b = Instantiate(healingBananaPrefab, pos, Quaternion.identity);
        b.Initialize(settings.healingBananaActivateDelay, settings.healingBananaHealAmount);

        healingBananaCooldownTimer = settings.healingBananaCooldown;
        return true;
    }

    public bool TryHit(int damage)
    {
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

    public void UpdateMoveAnim(string idleStateName, string walkStateName)
    {
        if (Anim == null) return;

        float vx = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        if (vx > 0.05f) Anim.Play(walkStateName);
        else Anim.Play(idleStateName);
    }

    public float GetAnimLength(string stateName)
    {
        if (Anim == null)
        {
            Debug.LogError("PlayerController: Animator is missing.");
            return 0f;
        }

        AnimatorStateInfo current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        AnimatorStateInfo next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Anim.Update(0f);

        current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Debug.LogError($"PlayerController: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }
    public void NotifyBananaPeelDestroyed()
    {
        activeBananaPeelCount--;
        if (activeBananaPeelCount < 0)
            activeBananaPeelCount = 0;
    }

    public Rigidbody2D Rigidbody => rb;
    public BoxCollider2D BoxCollider => boxCol;
}