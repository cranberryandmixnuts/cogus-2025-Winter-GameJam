using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    private enum StunAnimPhase
    {
        Slip,
        Groggy,
        Stand
    }

    [Header("Ranges")]
    [SerializeField] private Collider2D backOffRange;

    [Header("Attack Geometry")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float swingStartAngleDeg = 80f;
    [SerializeField] private float swingEndAngleDeg = -75f;
    [SerializeField] private float swingLength = 2f;
    [SerializeField] private LayerMask playerHitMask;

    [Header("Attack Timing (Percent)")]
    [SerializeField, Range(0f, 1f)] private float attackPrepPercent = 0.3f;
    [SerializeField, Range(0f, 1f)] private float attackEndPercent = 0.9f;

    [Header("Attack Cooldown")]
    [SerializeField] private Vector2 attackCooldownRange = new(1.5f, 3f);

    [Header("Gizmos")]
    [SerializeField] private bool drawSwingConeGizmo = true;
    [SerializeField] private Color swingConeFillColor = new(1f, 0.7f, 0.2f, 0.12f);
    [SerializeField] private Color swingConeWireColor = new(1f, 0.7f, 0.2f, 0.9f);

    [Header("Animation")]
    [SerializeField] private Animator Anim;
    [SerializeField] private LineRenderer swingLine;
    [SerializeField] private GameObject stunStar;

    private const string idleStateName = "monster_mantis_Idle";
    private const string walkStateName = "monster_mantis_Walk";
    private const string attackStateName = "monster_mantis_Attack";
    private const string slipStateName = "monster_mantis_Slip";
    private const string groggyStateName = "monster_mantis_Groggy";
    private const string standStateName = "monster_mantis_Stand";

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

    private float stunEndTime;
    private bool stunAnimActive;
    private StunAnimPhase stunAnimPhase;
    private float slipTimer;

    private int idleHash;
    private int walkHash;
    private int attackHash;
    private int slipHash;
    private int groggyHash;
    private int standHash;

    private float slipLength;
    private float standLength;

    protected override void Start()
    {
        base.Start();

        if (Anim == null) Anim = GetComponent<Animator>();

        CacheAnimationRefs();

        cachedPlayer = PlayerController.Instance;

        facingDir = 1;
        attackCooldownTimer = 0f;

        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;

        state = MantisState.Walk;

        stunAnimActive = false;
        stunAnimPhase = StunAnimPhase.Slip;
        slipTimer = 0f;
        stunEndTime = 0f;

        ClearSwingLine();
        PlayNormalAnimForState(state);

        if (stunStar != null) stunStar.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer < 0f) attackCooldownTimer = 0f;

        TickStunVisual();
        TickAnimation();

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

    public override bool ApplyStun(float duration)
    {
        bool changed = base.ApplyStun(duration);
        if (!changed) return false;

        stunEndTime = Time.time + duration;

        if (IsDead) return true;
        if (state == MantisState.Dead) return true;

        if (!stunAnimActive)
        {
            stunAnimActive = true;
            stunAnimPhase = StunAnimPhase.Slip;
            slipTimer = Mathf.Max(0.01f, slipLength);

            if (Anim != null)
            {
                Anim.speed = 1f;
                Anim.Play(slipHash, 0, 0f);
            }

            return true;
        }

        if (stunAnimPhase == StunAnimPhase.Stand)
        {
            float rem = GetStunRemaining();
            if (rem > Mathf.Max(0.01f, standLength) + 0.05f)
            {
                StartGroggy();
                stunAnimPhase = StunAnimPhase.Groggy;
            }
        }

        return true;
    }

    private void TickAnimation()
    {
        if (Anim == null) return;
        if (IsDead) return;
        if (state == MantisState.Dead) return;

        if (IsStunned())
        {
            TickStunAnimation();
            return;
        }

        if (stunAnimActive)
        {
            stunAnimActive = false;
            Anim.speed = 1f;
        }

        if (state != MantisState.Attack)
            PlayNormalAnimForState(state);
    }

    private void TickStunAnimation()
    {
        float rem = GetStunRemaining();
        float sLen = Mathf.Max(0.01f, standLength);

        if (!stunAnimActive)
        {
            stunAnimActive = true;
            stunAnimPhase = StunAnimPhase.Slip;
            slipTimer = Mathf.Max(0.01f, slipLength);

            Anim.speed = 1f;
            Anim.Play(slipHash, 0, 0f);
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Slip)
        {
            slipTimer -= Time.deltaTime;
            if (slipTimer > 0f) return;

            if (rem <= sLen)
            {
                StartStandToEndExactlyAtStunEnd(rem);
                stunAnimPhase = StunAnimPhase.Stand;
                return;
            }

            StartGroggy();
            stunAnimPhase = StunAnimPhase.Groggy;
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Groggy)
        {
            if (rem <= sLen)
            {
                StartStandToEndExactlyAtStunEnd(rem);
                stunAnimPhase = StunAnimPhase.Stand;
                return;
            }

            EnsurePlaying(groggyHash);
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Stand)
        {
            if (rem > sLen + 0.05f)
            {
                StartGroggy();
                stunAnimPhase = StunAnimPhase.Groggy;
            }
        }
    }

    private void StartGroggy()
    {
        if (Anim == null) return;

        Anim.speed = 1f;
        Anim.Play(groggyHash, 0, 0f);
    }

    private void StartStandToEndExactlyAtStunEnd(float stunRemaining)
    {
        if (Anim == null) return;

        float sLen = Mathf.Max(0.01f, standLength);
        float rem = Mathf.Max(0.01f, stunRemaining);

        float speed = sLen / rem;

        Anim.speed = speed;
        Anim.Play(standHash, 0, 0f);
    }

    private void EnsurePlaying(int stateHash)
    {
        AnimatorStateInfo info = Anim.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == stateHash) return;

        Anim.Play(stateHash, 0, 0f);
    }

    private void TickStunVisual()
    {
        if (stunStar == null) return;

        bool active = !IsDead && state != MantisState.Dead && IsStunned();
        if (stunStar.activeSelf != active) stunStar.SetActive(active);
    }

    private float GetStunRemaining()
    {
        float rem = stunEndTime - Time.time;
        if (rem < 0f) rem = 0f;
        return rem;
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

        if (!IsStunned())
            PlayNormalAnimForState(state);
    }

    private void BeginAttack()
    {
        if (Anim == null) return;

        Anim.speed = 1f;
        Anim.Play(attackHash, 0, 0f);

        float clipLen = GetAnimLength(attackStateName);

        float prepPercent = attackPrepPercent;
        float endPercent = attackEndPercent;

        if (prepPercent >= endPercent)
        {
            prepPercent = 0.25f;
            endPercent = 0.75f;
        }

        attackWindupRuntime = clipLen * prepPercent;
        swingDurationRuntime = clipLen * (endPercent - prepPercent);
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

        GetSwingAngleRange(facingDir, out float minAng, out float maxAng);

        if (ang < minAng) return false;
        if (ang > maxAng) return false;

        return true;
    }

    private void GetSwingAngleRange(int facing, out float minAng, out float maxAng)
    {
        if (facing >= 0)
        {
            minAng = Mathf.Min(swingStartAngleDeg, swingEndAngleDeg);
            maxAng = Mathf.Max(swingStartAngleDeg, swingEndAngleDeg);
            return;
        }

        float a = -swingStartAngleDeg;
        float b = -swingEndAngleDeg;
        minAng = Mathf.Min(a, b);
        maxAng = Mathf.Max(a, b);
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
        transform.rotation = Quaternion.Euler(0f, facingDir == 1 ? 180f : 0f, 0f);
    }

    private PlayerController GetPlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        cachedPlayer = PlayerController.Instance;
        return cachedPlayer;
    }

    private void PlayNormalAnimForState(MantisState s)
    {
        if (Anim == null) return;
        if (IsStunned()) return;

        if (s == MantisState.Idle) EnsurePlaying(idleHash);
        else if (s == MantisState.Walk) EnsurePlaying(walkHash);
        else if (s == MantisState.BackWalk) EnsurePlaying(walkHash);
    }

    private void CacheAnimationRefs()
    {
        idleHash = Animator.StringToHash(idleStateName);
        walkHash = Animator.StringToHash(walkStateName);
        attackHash = Animator.StringToHash(attackStateName);
        slipHash = Animator.StringToHash(slipStateName);
        groggyHash = Animator.StringToHash(groggyStateName);
        standHash = Animator.StringToHash(standStateName);

        slipLength = FindClipLengthByName(slipStateName);
        standLength = FindClipLengthByName(standStateName);
    }

    public float GetAnimLength(string stateName)
    {
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

        Debug.LogError($"MantisEnemy: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }

    private float FindClipLengthByName(string clipName)
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

            return c.length;
        }

        return 0f;
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawSwingConeGizmo) return;

        float radius = swingLength;
        if (radius <= 0f) return;

        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;

        int dir = GetPreviewFacingDir();
        GetSwingAngleRange(dir, out float minAng, out float maxAng);

        float sweep = maxAng - minAng;
        if (sweep <= 0f) return;

        Vector3 forward = Vector3.right * dir;

        Vector3 from = Quaternion.AngleAxis(minAng, Vector3.forward) * forward;

        Handles.color = swingConeFillColor;
        Handles.DrawSolidArc(origin, Vector3.forward, from, sweep, radius);

        Handles.color = swingConeWireColor;
        Handles.DrawWireArc(origin, Vector3.forward, from, sweep, radius);

        Vector3 edgeA = origin + (Quaternion.AngleAxis(minAng, Vector3.forward) * forward).normalized * radius;
        Vector3 edgeB = origin + (Quaternion.AngleAxis(maxAng, Vector3.forward) * forward).normalized * radius;

        Handles.DrawLine(origin, edgeA);
        Handles.DrawLine(origin, edgeB);

        float dot = HandleUtility.GetHandleSize(origin) * 0.03f;
        Handles.DrawSolidDisc(origin, Vector3.forward, dot);
    }

    private int GetPreviewFacingDir()
    {
        if (Application.isPlaying)
        {
            if (facingDir == 0) return 1;
            return facingDir;
        }

        float y = transform.eulerAngles.y % 360f;
        if (y > 90f && y < 270f) return 1;
        return -1;
    }
#endif

    protected override void OnDied()
    {
        state = MantisState.Dead;
        ClearSwingLine();
        StopHorizontal();

        stunStar.SetActive(false);

        Anim.speed = 1f;
    }
}