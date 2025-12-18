using UnityEngine;

public sealed class BananaEnemy : EnemyBase
{
    private enum BananaState
    {
        Move,
        Dead
    }

    private enum AnimPhase
    {
        Walk,
        StunFall,
        StunHold,
        StunStand
    }

    [Header("Movement")]
    [SerializeField] private float horizontalAcceleration = 18f;
    [SerializeField] private float directionDecisionInterval = 0.08f;
    [SerializeField] private float directionDecisionChanceMultiplier = 1f;

    [Header("Attack")]
    [SerializeField] private EnemyBananaPeel peelPrefab;
    [SerializeField] private Transform peelSpawnOrigin;
    [SerializeField] private Vector2 peelDropCooldownRange = new(1.2f, 2.6f);

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject stunStar;

    private const string walkStateName = "몬스터바나나걷는거";
    private const string fallStateName = "몬스터바나나넘어지는거";
    private const string standStateName = "몬스터바나나일어나는거";

    private BananaState state;

    private int moveDir;
    private float directionDecisionTimer;
    private float peelTimer;

    private Camera cachedCamera;

    private AnimPhase animPhase;
    private float fallTimer;
    private float fallLength;
    private float standLength;

    private int walkHash;
    private int fallHash;
    private int standHash;

    protected override void Start()
    {
        base.Start();

        cachedCamera = Camera.main;

        state = BananaState.Move;

        moveDir = transform.position.x >= 0f ? -1 : 1;
        ApplyFacingByMoveDir();

        directionDecisionTimer = 0f;
        ResetPeelTimer();

        CacheAnimationRefs();
        animPhase = AnimPhase.Walk;
        PlayWalk();

        if (stunStar != null) stunStar.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();

        TickStunVisual();
        TickAnimation();

        if (IsDead) return;
        if (state == BananaState.Dead) return;
        if (IsStunned()) return;

        TickDirectionDecision();
        TickPeelDrop();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        if (state == BananaState.Dead) return;

        if (IsStunned())
        {
            StopHorizontal();
            return;
        }

        EnemySetting s = Setting;
        if (s == null)
        {
            StopHorizontal();
            return;
        }

        Rigidbody2D rb = Rigidbody;

        float targetSpeed = moveDir * Mathf.Max(0f, s.moveSpeed);
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, Mathf.Max(0f, horizontalAcceleration) * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    public override bool ApplyStun(float duration)
    {
        bool changed = base.ApplyStun(duration);
        if (!changed) return false;

        if (IsDead) return true;
        if (state == BananaState.Dead) return true;

        if (IsStunned() && animPhase == AnimPhase.StunStand)
        {
            float sLen = Mathf.Max(0.01f, standLength);
            if (StunRemaining > sLen + 0.05f)
            {
                HoldFallen();
                animPhase = AnimPhase.StunHold;
            }
        }

        return true;
    }

    private void TickDirectionDecision()
    {
        directionDecisionTimer -= Time.deltaTime;
        if (directionDecisionTimer > 0f) return;

        directionDecisionTimer = Mathf.Max(0.01f, directionDecisionInterval);

        float chance = GetEdgeNormalized01();
        chance *= Mathf.Max(0f, directionDecisionChanceMultiplier);
        chance = Mathf.Clamp01(chance);

        if (Random.value < chance)
        {
            int desired = transform.position.x >= 0f ? -1 : 1;
            if (moveDir != desired)
            {
                moveDir = desired;
                ApplyFacingByMoveDir();
            }
        }
    }

    private float GetEdgeNormalized01()
    {
        Camera cam = cachedCamera != null ? cachedCamera : Camera.main;
        if (cam == null) return 0f;

        Vector3 v = cam.WorldToViewportPoint(transform.position);
        float centerDist = Mathf.Abs(v.x - 0.5f) / 0.5f;
        return Mathf.Clamp01(centerDist);
    }

    private void TickPeelDrop()
    {
        peelTimer -= Time.deltaTime;
        if (peelTimer > 0f) return;

        DropPeel();
        ResetPeelTimer();
    }

    private void ResetPeelTimer()
    {
        float a = peelDropCooldownRange.x;
        float b = peelDropCooldownRange.y;

        if (b < a)
            (a, b) = (b, a);

        peelTimer = Random.Range(a, b);
        if (peelTimer < 0f) peelTimer = 0f;
    }

    private void DropPeel()
    {
        EnemyBananaPeel prefab = peelPrefab;
        if (prefab == null) return;

        Vector3 pos = peelSpawnOrigin != null ? peelSpawnOrigin.position : transform.position;

        EnemyBananaPeel peel = Instantiate(prefab, pos, Quaternion.identity);

        int damage = 0;
        EnemySetting s = Setting;
        if (s != null) damage = Mathf.Max(0, s.attackDamage);

        peel.Initialize(damage);
    }

    private void ApplyFacingByMoveDir()
    {
        int face = moveDir >= 0 ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, face == -1 ? 180f : 0f, 0f);
    }

    protected override void OnDied()
    {
        state = BananaState.Dead;
        StopHorizontal();

        if (stunStar != null) stunStar.SetActive(false);

        if (animator != null) animator.speed = 1f;
    }

    private void TickStunVisual()
    {
        if (stunStar == null) return;

        bool active = !IsDead && state != BananaState.Dead && IsStunned();
        if (stunStar.activeSelf != active) stunStar.SetActive(active);
    }

    private void TickAnimation()
    {
        if (animator == null) return;
        if (IsDead || state == BananaState.Dead) return;

        if (!IsStunned())
        {
            if (animPhase != AnimPhase.Walk)
            {
                animPhase = AnimPhase.Walk;
                PlayWalk();
            }

            return;
        }

        float fLen = Mathf.Max(0.01f, fallLength);
        float sLen = Mathf.Max(0.01f, standLength);

        if (animPhase == AnimPhase.Walk)
        {
            animPhase = AnimPhase.StunFall;
            fallTimer = fLen;
            PlayFall();
            return;
        }

        if (animPhase == AnimPhase.StunFall)
        {
            fallTimer -= Time.deltaTime;
            if (fallTimer > 0f) return;

            HoldFallen();
            animPhase = AnimPhase.StunHold;
            return;
        }

        if (animPhase == AnimPhase.StunHold)
        {
            if (StunRemaining > sLen) return;

            StartStandToEndExactlyAtStunEnd();
            animPhase = AnimPhase.StunStand;
            return;
        }

        if (animPhase == AnimPhase.StunStand)
        {
            if (StunRemaining > sLen + 0.05f)
            {
                HoldFallen();
                animPhase = AnimPhase.StunHold;
            }
        }
    }

    private void CacheAnimationRefs()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        walkHash = Animator.StringToHash(walkStateName);
        fallHash = Animator.StringToHash(fallStateName);
        standHash = Animator.StringToHash(standStateName);

        fallLength = FindClipLengthByName(fallStateName);
        standLength = FindClipLengthByName(standStateName);

        if (fallLength <= 0f) fallLength = 0.25f;
        if (standLength <= 0f) standLength = 0.25f;
    }

    private float FindClipLengthByName(string clipName)
    {
        if (animator == null) return 0f;

        RuntimeAnimatorController c = animator.runtimeAnimatorController;
        if (c == null) return 0f;

        AnimationClip[] clips = c.animationClips;
        if (clips == null) return 0f;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) continue;
            if (clip.name == clipName) return clip.length;
        }

        return 0f;
    }

    private void PlayWalk()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(walkHash, 0, 0f);
    }

    private void PlayFall()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(fallHash, 0, 0f);
    }

    private void HoldFallen()
    {
        if (animator == null) return;

        animator.speed = 0f;
        animator.Play(fallHash, 0, 0.999f);
        animator.Update(0f);
    }

    private void StartStandToEndExactlyAtStunEnd()
    {
        if (animator == null) return;

        float sLen = Mathf.Max(0.01f, standLength);
        float rem = Mathf.Max(0.01f, StunRemaining);

        float speed = sLen / rem;

        animator.speed = speed;
        animator.Play(standHash, 0, 0f);
    }
}