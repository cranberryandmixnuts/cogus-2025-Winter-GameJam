using UnityEngine;

public sealed class MantisEnemy : EnemyBase
{
    private enum MantisState
    {
        Idle,
        Walk,
        Chase,
        BackWalk,
        Attack,
        Dead
    }

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimChase = "Chase";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimAttack = "Attack";

    [Header("Ranges")]
    [SerializeField] private Collider2D walkRange;
    [SerializeField] private Collider2D backOffRange;

    [Header("Movement")]
    [SerializeField] private float chaseSpeedMultiplier = 2.6f;

    [Header("Attack Geometry")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float swingStartAngleDeg = 75f;
    [SerializeField] private float swingEndAngleDeg = -30f;
    [SerializeField] private float swingLength = 2f;
    [SerializeField] private LayerMask playerHitMask;

    [Header("Attack Timing (Percent)")]
    [SerializeField, Range(0f, 1f)] private float attackPrepPercent = 0.2f;
    [SerializeField, Range(0f, 1f)] private float attackEndPercent = 0.9f;

    [Header("Attack Cooldown")]
    [SerializeField] private Vector2 attackCooldownRange = new(1.5f, 3f);

    [Header("Optional")]
    [SerializeField] private Animator Anim;
    [SerializeField] private LineRenderer swingLine;

    private MantisState state;

    private int facingDir = 1;
    private float attackCooldownTimer;

    private int attackPhase;
    private float attackPhaseTimer;
    private bool attackResolved;

    private float attackWindupRuntime;
    private float swingDurationRuntime;
    private float attackRecoverRuntime;

    private PlayerController cachedPlayer;

    protected override void Start()
    {
        base.Start();

        cachedPlayer = PlayerController.Instance;

        state = MantisState.Chase;
        attackCooldownTimer = 0f;

        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;

        ClearSwingLine();
        PlayAnimForState(state);
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        if (state == MantisState.Attack)
        {
            if (IsStunned())
            {
                CancelAttackAndStartCooldown();
                return;
            }

            TickAttack();
            return;
        }

        if (IsStunned()) return;

        FacePlayer();

        switch (state)
        {
            case MantisState.Idle:
            case MantisState.Walk:
            case MantisState.Chase:
                ChooseMovementState();
                break;

            case MantisState.BackWalk:
                TickBackWalk();
                break;

            case MantisState.Dead:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (state == MantisState.Attack)
        {
            StopHorizontal();
            return;
        }

        if (IsStunned())
        {
            StopHorizontal();
            return;
        }

        EnemySetting s = Setting;
        PlayerController p = GetPlayer();

        if (s == null || p == null)
        {
            StopHorizontal();
            return;
        }

        float baseSpeed = Mathf.Max(0f, s.moveSpeed);
        float walkSpeed = baseSpeed;
        float chaseSpeed = baseSpeed * Mathf.Max(0f, chaseSpeedMultiplier);

        switch (state)
        {
            case MantisState.Walk:
                MoveTowardsPlayer(p, walkSpeed);
                break;

            case MantisState.Chase:
                MoveTowardsPlayer(p, chaseSpeed);
                break;

            case MantisState.BackWalk:
                MoveAwayFromPlayer(p, walkSpeed);
                break;

            default:
                StopHorizontal();
                break;
        }
    }

    private void TickBackWalk()
    {
        if (attackCooldownTimer <= 0f && IsPlayerInSwingCone())
        {
            EnterState(MantisState.Attack);
            return;
        }

        if (!InBackOffRange() || !IsPlayerInSwingCone()) ChooseMovementState();
    }

    private void ChooseMovementState()
    {
        if (state == MantisState.Dead) return;

        bool inCone = IsPlayerInSwingCone();
        bool inWalk = InWalkRange();
        bool inBack = InBackOffRange();

        MantisState nextState;

        if (inCone)
        {
            if (attackCooldownTimer <= 0f)
            {
                nextState = MantisState.Attack;
            }
            else
            {
                if (inBack)
                    nextState = MantisState.BackWalk;
                else
                    nextState = MantisState.Idle;
            }
        }
        else
        {
            if (inWalk)
                nextState = MantisState.Walk;
            else
                nextState = MantisState.Chase;
        }

        EnterState(nextState);
    }

    private void EnterState(MantisState newState)
    {
        if (state == newState) return;

        state = newState;

        if (state == MantisState.Attack)
        {
            BeginAttack();
            return;
        }

        ClearSwingLine();
        PlayAnimForState(state);
    }

    private void BeginAttack()
    {
        PlayAnimForState(MantisState.Attack);

        float clipLen = GetClipLength(AnimAttack);
        if (clipLen <= 0f) clipLen = 1f;

        if (attackPrepPercent >= attackEndPercent)
        {
            attackPrepPercent = 0.25f;
            attackEndPercent = 0.75f;
        }

        attackWindupRuntime = clipLen * attackPrepPercent;
        swingDurationRuntime = clipLen * (attackEndPercent - attackPrepPercent);
        attackRecoverRuntime = clipLen - (attackWindupRuntime + swingDurationRuntime);
        if (attackRecoverRuntime < 0.01f) attackRecoverRuntime = 0.01f;

        attackPhase = 0;
        attackPhaseTimer = attackWindupRuntime;
        attackResolved = false;

        StopHorizontal();
        ClearSwingLine();
    }

    private void TickAttack()
    {
        if (attackPhase == 0)
        {
            ClearSwingLine();
            attackPhaseTimer -= Time.deltaTime;

            if (attackPhaseTimer <= 0f)
            {
                attackPhase = 1;
                attackPhaseTimer = swingDurationRuntime;
                attackResolved = false;
            }
            return;
        }

        if (attackPhase == 1)
        {
            PerformSwingStep();
            attackPhaseTimer -= Time.deltaTime;

            if (attackPhaseTimer <= 0f)
            {
                attackPhase = 2;
                attackPhaseTimer = attackRecoverRuntime;

                StartAttackCooldown();
                ClearSwingLine();
            }
            return;
        }

        if (attackPhase == 2)
        {
            ClearSwingLine();
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f) ChooseMovementState();
        }
    }

    private void CancelAttackAndStartCooldown()
    {
        StartAttackCooldown();
        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = true;
        ClearSwingLine();

        EnterState(MantisState.Idle);
    }

    private void StartAttackCooldown()
    {
        float min = attackCooldownRange.x;
        float max = attackCooldownRange.y;

        if (max < min) (max, min) = (min, max);

        attackCooldownTimer = Random.Range(min, max);
        if (attackCooldownTimer < 0f) attackCooldownTimer = 0f;
    }

    private void PerformSwingStep()
    {
        if (attackResolved) return;

        PlayerController p = GetPlayer();
        if (p == null) return;

        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        float norm = 1f - (attackPhaseTimer / swingDurationRuntime);
        if (norm < 0f) norm = 0f;
        if (norm > 1f) norm = 1f;

        float angleDeg = Mathf.Lerp(swingStartAngleDeg, swingEndAngleDeg, norm);
        if (facingDir < 0) angleDeg = -angleDeg;

        Vector2 dir = DirFromAngle(angleDeg);
        RaycastHit2D hit = Physics2D.Raycast(originPos, dir, swingLength, playerHitMask);

        if (hit.collider != null)
        {
            int damage = 0;
            EnemySetting s = Setting;
            if (s != null) damage = Mathf.Max(0, s.attackDamage);

            if (damage > 0 && p.TryHit(damage))
            {
                UpdateSwingLine(originPos, dir, hit.distance);
                attackResolved = true;
                StartAttackCooldown();
                ClearSwingLine();
                return;
            }

            UpdateSwingLine(originPos, dir, swingLength);
            return;
        }

        UpdateSwingLine(originPos, dir, swingLength);
    }

    private bool InWalkRange()
    {
        PlayerController p = GetPlayer();
        if (p == null) return false;
        if (walkRange == null) return false;
        return walkRange.OverlapPoint(p.transform.position);
    }

    private bool InBackOffRange()
    {
        PlayerController p = GetPlayer();
        if (p == null) return false;
        if (backOffRange == null) return false;
        return backOffRange.OverlapPoint(p.transform.position);
    }

    private bool IsPlayerInSwingCone()
    {
        PlayerController p = GetPlayer();
        if (p == null) return false;

        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)p.transform.position - originPos;

        float dist = toPlayer.magnitude;
        if (dist > swingLength) return false;

        Vector2 forward = Vector2.right * facingDir;
        float ang = Vector2.SignedAngle(forward, toPlayer);

        float minAng;
        float maxAng;

        if (facingDir >= 0)
        {
            minAng = Mathf.Min(swingStartAngleDeg, swingEndAngleDeg);
            maxAng = Mathf.Max(swingStartAngleDeg, swingEndAngleDeg);
        }
        else
        {
            float a = -swingStartAngleDeg;
            float b = -swingEndAngleDeg;
            minAng = Mathf.Min(a, b);
            maxAng = Mathf.Max(a, b);
        }

        if (ang < minAng || ang > maxAng) return false;
        return true;
    }

    private Vector2 DirFromAngle(float angleDeg)
    {
        Vector2 forward = Vector2.right * facingDir;
        Quaternion rot = Quaternion.AngleAxis(angleDeg, Vector3.forward);
        Vector2 dir = rot * forward;
        return dir.normalized;
    }

    private void MoveTowardsPlayer(PlayerController p, float speed)
    {
        float dx = p.transform.position.x - transform.position.x;

        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Rigidbody2D rb = Rigidbody;
        rb.linearVelocity = new Vector2(dirSign * speed, rb.linearVelocity.y);
    }

    private void MoveAwayFromPlayer(PlayerController p, float speed)
    {
        float dx = p.transform.position.x - transform.position.x;

        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Rigidbody2D rb = Rigidbody;
        rb.linearVelocity = new Vector2(-dirSign * speed, rb.linearVelocity.y);
    }

    private void FacePlayer()
    {
        PlayerController p = GetPlayer();
        if (p == null) return;

        facingDir = (p.transform.position.x - transform.position.x) >= 0f ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, facingDir == -1 ? 180f : 0f, 0f);
    }

    private PlayerController GetPlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        cachedPlayer = PlayerController.Instance;
        return cachedPlayer;
    }

    private float GetClipLength(string clipName)
    {
        if (Anim == null) return 0f;

        RuntimeAnimatorController ctrl = Anim.runtimeAnimatorController;
        if (ctrl == null) return 0f;

        AnimationClip[] clips = ctrl.animationClips;
        if (clips == null) return 0f;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip c = clips[i];
            if (c == null) continue;
            if (c.name != clipName) continue;

            float speed = Anim.speed;
            if (speed <= 0f) return Mathf.Infinity;
            return c.length / speed;
        }

        return 0f;
    }

    private void PlayAnimForState(MantisState s)
    {
        if (Anim == null) return;

        switch (s)
        {
            case MantisState.Idle:
                Anim.Play(AnimIdle);
                break;
            case MantisState.Walk:
                Anim.Play(AnimWalk);
                break;
            case MantisState.Chase:
                Anim.Play(AnimChase);
                break;
            case MantisState.BackWalk:
                Anim.Play(AnimBackWalk);
                break;
            case MantisState.Attack:
                Anim.Play(AnimAttack);
                break;
        }
    }

    private void UpdateSwingLine(Vector2 origin, Vector2 dir, float length)
    {
        if (swingLine == null) return;

        swingLine.positionCount = 2;
        swingLine.SetPosition(0, origin);
        swingLine.SetPosition(1, origin + dir.normalized * length);
    }

    private void ClearSwingLine()
    {
        if (swingLine == null) return;
        swingLine.positionCount = 0;
    }

    protected override void OnDied()
    {
        state = MantisState.Dead;
        ClearSwingLine();
        StopHorizontal();
    }
}