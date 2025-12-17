using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public sealed class MantisEnemy : EnemyBase
{
    private enum MantisState
    {
        Idle,
        Walk,
        BackWalk,
        Attack,
        Dead
    }

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimAttack = "Attack";

    [Header("Ranges")]
    [SerializeField] private Collider2D backOffRange;

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
    [SerializeField] private Vector2 attackCooldownRange = new Vector2(1.5f, 3f);

    [Header("Optional")]
    [SerializeField] private Animator Anim;
    [SerializeField] private LineRenderer swingLine;

    private MantisState state;

    private int facingDir;
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

        facingDir = 1;
        attackCooldownTimer = 0f;

        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;

        state = MantisState.Walk;

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
            if (IsStunned()) CancelAttackAndStartCooldown();
            else TickAttack();

            return;
        }

        if (IsStunned()) return;

        FacePlayer();
        ChooseMovementState();
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

        float speed = s.moveSpeed;
        if (speed < 0f) speed = 0f;

        if (state == MantisState.Walk) MoveTowardsPlayer(p, speed);
        else if (state == MantisState.BackWalk) MoveAwayFromPlayer(p, speed);
        else StopHorizontal();
    }

    private void ChooseMovementState()
    {
        if (state == MantisState.Dead) return;

        bool inBack = InBackOffRange();
        bool inCone = IsPlayerInSwingCone();

        MantisState next;

        if (inCone)
        {
            if (attackCooldownTimer <= 0f) next = MantisState.Attack;
            else if (inBack) next = MantisState.BackWalk;
            else next = MantisState.Idle;
        }
        else
        {
            if (inBack) next = MantisState.BackWalk;
            else next = MantisState.Walk;
        }

        EnterState(next);
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
            EnemySetting s = Setting;
            int damage = 0;

            if (s != null) damage = s.attackDamage;
            if (damage < 0) damage = 0;

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

        if (ang < minAng) return false;
        if (ang > maxAng) return false;

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

            float spd = Anim.speed;
            if (spd <= 0f) return Mathf.Infinity;

            return c.length / spd;
        }

        return 0f;
    }

    private void PlayAnimForState(MantisState s)
    {
        if (Anim == null) return;

        if (s == MantisState.Idle) Anim.Play(AnimIdle);
        else if (s == MantisState.Walk) Anim.Play(AnimWalk);
        else if (s == MantisState.BackWalk) Anim.Play(AnimBackWalk);
        else if (s == MantisState.Attack) Anim.Play(AnimAttack);
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