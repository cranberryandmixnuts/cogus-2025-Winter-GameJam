using UnityEngine;

public sealed class BananaEnemy : EnemyBase
{
    private enum BananaState
    {
        Move,
        Dead
    }

    [Header("Movement")]
    [SerializeField] private float horizontalAcceleration = 18f;
    [SerializeField] private float directionDecisionInterval = 0.08f;
    [SerializeField] private float directionDecisionChanceMultiplier = 1f;

    [Header("Attack")]
    [SerializeField] private EnemyBananaPeel peelPrefab;
    [SerializeField] private Transform peelSpawnOrigin;
    [SerializeField] private Vector2 peelDropCooldownRange = new(1.2f, 2.6f);

    private BananaState state;

    private int moveDir;
    private float directionDecisionTimer;
    private float peelTimer;

    private Camera cachedCamera;

    protected override void Start()
    {
        base.Start();

        cachedCamera = Camera.main;

        state = BananaState.Move;

        moveDir = transform.position.x >= 0f ? -1 : 1;
        ApplyFacingByMoveDir();

        directionDecisionTimer = 0f;
        ResetPeelTimer();
    }

    protected override void Update()
    {
        base.Update();

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
    }
}