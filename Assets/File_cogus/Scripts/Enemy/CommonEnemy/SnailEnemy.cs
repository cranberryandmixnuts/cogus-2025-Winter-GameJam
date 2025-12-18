using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SnailEnemy : EnemyBase
{
    private enum SnailState
    {
        Crawl,
        Hide,
        Dead
    }

    private enum NormalAnimPhase
    {
        CrawlWalk,
        HideEnter,
        HideHold,
        HideExit
    }

    private enum StunAnimPhase
    {
        Fall,
        Hold,
        Stand
    }

    [Header("Colliders")]
    [SerializeField] private Collider2D detectRangeCollider;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject stunStar;

    private const string walkStateName = "몬스터달팽이가걷는거";
    private const string hideEnterStateName = "몬스터달팽이집들어가기";
    private const string hideExitStateName = "몬스터달팽이집나오기";
    private const string standStateName = "몬스터달팽이일어나는거";
    private const string fallStateName = "몬스터달팽이넘어지는거";

    private readonly Collider2D[] detectHits = new Collider2D[8];
    private readonly Collider2D[] bodyHits = new Collider2D[8];

    private SnailState state;
    private float contactCooldownTimer;
    private PlayerController cachedPlayer;

    private NormalAnimPhase normalAnimPhase;

    private bool stunAnimActive;
    private StunAnimPhase stunAnimPhase;
    private float fallTimer;

    private float stunEndTime;

    private int walkHash;
    private int hideEnterHash;
    private int hideExitHash;
    private int standHash;
    private int fallHash;

    private float hideEnterLength;
    private float hideExitLength;
    private float standLength;
    private float fallLength;

    private float hideEnterTimer;
    private float hideExitTimer;

    protected override bool IsInvincible
    {
        get { return state == SnailState.Hide; }
    }

    protected override void Start()
    {
        base.Start();

        if (detectRangeCollider == null)
        {
            Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                if (!cols[i].isTrigger) continue;

                detectRangeCollider = cols[i];
                break;
            }
        }

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        CacheAnimationRefs();

        cachedPlayer = PlayerController.Instance;
        SetState(SnailState.Crawl, true);

        if (stunStar != null) stunStar.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();

        if (contactCooldownTimer > 0f) contactCooldownTimer -= Time.deltaTime;
        if (contactCooldownTimer < 0f) contactCooldownTimer = 0f;

        TickStunVisual();
        TickAnimation();

        if (IsDead) return;
        if (state == SnailState.Dead) return;

        bool inRange = IsPlayerInDetectRange(out PlayerController p);

        if (state == SnailState.Hide)
        {
            if (!inRange) SetState(SnailState.Crawl, false);
        }
        else if (state == SnailState.Crawl)
        {
            if (inRange) SetState(SnailState.Hide, false);
        }

        if (p != null) cachedPlayer = p;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        if (state == SnailState.Dead) return;

        if (IsStunned())
        {
            StopHorizontal();
            return;
        }

        if (state == SnailState.Hide)
        {
            StopHorizontal();
            TryDealContactDamage();
            return;
        }

        MoveTowardPlayer();
    }

    public override bool ApplyStun(float duration)
    {
        bool changed = base.ApplyStun(duration);
        if (!changed) return false;

        stunEndTime = Time.time + duration;

        if (IsDead) return true;
        if (state == SnailState.Dead) return true;

        if (stunAnimActive && stunAnimPhase == StunAnimPhase.Stand)
        {
            float sLen = Mathf.Max(0.01f, standLength);
            if (GetStunRemaining() > sLen + 0.05f)
                ForceHoldFallen();
        }

        return true;
    }

    protected override void OnDied()
    {
        state = SnailState.Dead;

        if (stunStar != null) stunStar.SetActive(false);

        if (animator != null) animator.speed = 1f;
    }

    private void SetState(SnailState newState, bool immediate)
    {
        if (state == SnailState.Dead) return;

        SnailState prev = state;
        state = newState;

        if (IsDead)
        {
            state = SnailState.Dead;
            return;
        }

        if (immediate)
        {
            if (state == SnailState.Hide)
            {
                normalAnimPhase = NormalAnimPhase.HideEnter;
                StartHideEnter();
                return;
            }

            normalAnimPhase = NormalAnimPhase.CrawlWalk;
            StartWalk();
            return;
        }

        if (prev == SnailState.Crawl && newState == SnailState.Hide)
        {
            normalAnimPhase = NormalAnimPhase.HideEnter;
            StartHideEnter();
            return;
        }

        if (prev == SnailState.Hide && newState == SnailState.Crawl)
        {
            normalAnimPhase = NormalAnimPhase.HideExit;
            StartHideExit();
            return;
        }

        if (newState == SnailState.Hide)
        {
            normalAnimPhase = NormalAnimPhase.HideEnter;
            StartHideEnter();
            return;
        }

        normalAnimPhase = NormalAnimPhase.CrawlWalk;
        StartWalk();
    }

    private void TickAnimation()
    {
        if (animator == null) return;
        if (IsDead) return;
        if (state == SnailState.Dead) return;

        if (IsStunned())
        {
            TickStunAnimation();
            return;
        }

        if (stunAnimActive)
        {
            stunAnimActive = false;
            animator.speed = 1f;
        }

        TickNormalAnimation();
    }

    private void TickStunAnimation()
    {
        float fLen = Mathf.Max(0.01f, fallLength);
        float sLen = Mathf.Max(0.01f, standLength);
        float rem = GetStunRemaining();

        if (!stunAnimActive)
        {
            stunAnimActive = true;
            stunAnimPhase = StunAnimPhase.Fall;
            fallTimer = fLen;

            animator.speed = 1f;
            animator.Play(fallHash, 0, 0f);
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Fall)
        {
            fallTimer -= Time.deltaTime;
            if (fallTimer > 0f) return;

            ForceHoldFallen();
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Hold)
        {
            if (rem > sLen) return;

            StartStandToEndExactlyAtStunEnd(rem);
            stunAnimPhase = StunAnimPhase.Stand;
            return;
        }

        if (stunAnimPhase == StunAnimPhase.Stand)
        {
            if (rem > sLen + 0.05f)
                ForceHoldFallen();
        }
    }

    private void TickNormalAnimation()
    {
        if (state == SnailState.Hide)
        {
            if (normalAnimPhase == NormalAnimPhase.CrawlWalk || normalAnimPhase == NormalAnimPhase.HideExit)
            {
                normalAnimPhase = NormalAnimPhase.HideEnter;
                StartHideEnter();
            }
        }
        else
        {
            if (normalAnimPhase == NormalAnimPhase.HideEnter || normalAnimPhase == NormalAnimPhase.HideHold)
            {
                normalAnimPhase = NormalAnimPhase.HideExit;
                StartHideExit();
            }
        }

        if (normalAnimPhase == NormalAnimPhase.CrawlWalk)
        {
            if (animator.speed != 1f) animator.speed = 1f;
            if (!IsInState(walkHash)) animator.Play(walkHash, 0, 0f);
            return;
        }

        if (normalAnimPhase == NormalAnimPhase.HideEnter)
        {
            hideEnterTimer -= Time.deltaTime;
            if (hideEnterTimer > 0f) return;

            HoldHidden();
            normalAnimPhase = NormalAnimPhase.HideHold;
            return;
        }

        if (normalAnimPhase == NormalAnimPhase.HideHold)
        {
            HoldHidden();
            return;
        }

        if (normalAnimPhase == NormalAnimPhase.HideExit)
        {
            hideExitTimer -= Time.deltaTime;
            if (hideExitTimer > 0f) return;

            normalAnimPhase = NormalAnimPhase.CrawlWalk;
            StartWalk();
        }
    }

    private bool IsInState(int stateHash)
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash == stateHash;
    }

    private void StartWalk()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(walkHash, 0, 0f);
    }

    private void StartHideEnter()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(hideEnterHash, 0, 0f);

        hideEnterTimer = Mathf.Max(0.01f, hideEnterLength);
    }

    private void HoldHidden()
    {
        if (animator == null) return;

        animator.speed = 0f;
        animator.Play(hideEnterHash, 0, 0.999f);
        animator.Update(0f);
    }

    private void StartHideExit()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(hideExitHash, 0, 0f);

        hideExitTimer = Mathf.Max(0.01f, hideExitLength);
    }

    private void ForceHoldFallen()
    {
        if (animator == null) return;

        animator.speed = 0f;
        animator.Play(fallHash, 0, 0.999f);
        animator.Update(0f);

        stunAnimPhase = StunAnimPhase.Hold;
    }

    private void StartStandToEndExactlyAtStunEnd(float stunRemaining)
    {
        if (animator == null) return;

        float sLen = Mathf.Max(0.01f, standLength);
        float rem = Mathf.Max(0.01f, stunRemaining);

        float speed = sLen / rem;

        animator.speed = speed;
        animator.Play(standHash, 0, 0f);
    }

    private float GetStunRemaining()
    {
        float rem = stunEndTime - Time.time;
        if (rem < 0f) rem = 0f;
        return rem;
    }

    private void TickStunVisual()
    {
        if (stunStar == null) return;

        bool active = !IsDead && state != SnailState.Dead && IsStunned();
        if (stunStar.activeSelf != active) stunStar.SetActive(active);
    }

    private void CacheAnimationRefs()
    {
        walkHash = Animator.StringToHash(walkStateName);
        hideEnterHash = Animator.StringToHash(hideEnterStateName);
        hideExitHash = Animator.StringToHash(hideExitStateName);
        standHash = Animator.StringToHash(standStateName);
        fallHash = Animator.StringToHash(fallStateName);

        hideEnterLength = FindClipLengthByName(hideEnterStateName);
        hideExitLength = FindClipLengthByName(hideExitStateName);
        standLength = FindClipLengthByName(standStateName);
        fallLength = FindClipLengthByName(fallStateName);
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

    private void MoveTowardPlayer()
    {
        if (Setting == null)
        {
            StopHorizontal();
            return;
        }

        PlayerController p = GetPlayer();
        if (p == null)
        {
            StopHorizontal();
            return;
        }

        float dx = p.transform.position.x - transform.position.x;
        int dir = dx >= 0f ? 1 : -1;

        Rigidbody2D rb = Rigidbody;
        rb.linearVelocity = new Vector2(dir * Mathf.Max(0f, Setting.moveSpeed), rb.linearVelocity.y);

        transform.rotation = Quaternion.Euler(0f, dir == 1 ? 180f : 0f, 0f);
    }

    private PlayerController GetPlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        cachedPlayer = PlayerController.Instance;
        return cachedPlayer;
    }

    private bool IsPlayerInDetectRange(out PlayerController player)
    {
        player = null;

        if (detectRangeCollider == null) return false;

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = playerLayer,
            useTriggers = true
        };

        int count = detectRangeCollider.Overlap(filter, detectHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = detectHits[i];
            if (c == null) continue;

            PlayerController p = c.GetComponentInParent<PlayerController>();
            if (p == null) continue;

            player = p;
            return true;
        }

        return false;
    }

    private void TryDealContactDamage()
    {
        if (Setting == null) return;
        if (Setting.attackDamage <= 0) return;
        if (contactCooldownTimer > 0f) return;

        if (bodyCollider == null) return;

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = playerLayer,
            useTriggers = true
        };

        int count = bodyCollider.Overlap(filter, bodyHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = bodyHits[i];
            if (c == null) continue;

            PlayerController p = c.GetComponentInParent<PlayerController>();
            if (p == null) continue;

            bool hit = p.TryHit(Setting.attackDamage);
            if (hit) contactCooldownTimer = 0.1f;

            return;
        }
    }
}