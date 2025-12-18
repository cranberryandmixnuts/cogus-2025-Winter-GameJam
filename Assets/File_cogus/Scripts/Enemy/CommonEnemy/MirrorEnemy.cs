using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public sealed class MirrorEnemy : EnemyBase
{
    private enum MirrorState
    {
        Move,
        Attack,
        Dead
    }

    [Header("Colliders")]
    [SerializeField] private Collider2D tooCloseDetectCollider;
    [SerializeField] private LayerMask playerLayer;

    [Header("Hover")]
    [SerializeField] private Animator Anim;
    [SerializeField] private Transform hoverVisualTransform;
    [SerializeField] private float hoverAmplitude = 0.18f;
    [SerializeField] private float hoverFrequency = 1.6f;

    [Header("Aim")]
    [SerializeField] private float aimYOffset = 1f;

    [Header("Attack Cycle")]
    [SerializeField] private MirrorProjectile projectilePrefab;
    [SerializeField] private float attackDuration = 3f;
    [SerializeField] private int shotsPerAttack = 12;
    [SerializeField] private Vector2 cooldownRange = new(1f, 3f);
    [SerializeField] private Vector2 projectileSpawnYRange = new(-1f, 1f);

    private readonly Collider2D[] detectHits = new Collider2D[8];

    private MirrorState state;

    private float hoverPhase;
    private float hoverTime;
    private Vector3 hoverBaseLocalPos;

    private float cooldownTimer;

    private float attackTimer;
    private float shotInterval;
    private int shotIndex;
    private Vector2 lockedShotDir;

    private PlayerController cachedPlayer;

    protected override void Start()
    {
        base.Start();

        if (hoverVisualTransform == null) hoverVisualTransform = transform;

        hoverBaseLocalPos = hoverVisualTransform.localPosition;
        hoverPhase = Random.Range(0f, 10f);
        hoverTime = 0f;

        cachedPlayer = PlayerController.Instance;

        state = MirrorState.Move;
        ResetCooldown();
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        if (state == MirrorState.Attack)
        {
            if (IsStunned())
            {
                EndAttackAndStartCooldown();
                return;
            }

            TickAttack();
            return;
        }

        TickHover();
        TickCooldownAndMaybeStartAttack();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (state == MirrorState.Attack)
        {
            StopHorizontal();
            return;
        }

        if (IsStunned())
        {
            StopHorizontal();
            return;
        }

        TickMove();
    }

    private void TickMove()
    {
        EnemySetting s = Setting;
        if (s == null)
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

        bool tooClose = IsPlayerInTooCloseRange(out PlayerController detected);
        if (detected != null) cachedPlayer = detected;

        float px = p.transform.position.x;
        float ex = transform.position.x;

        int faceDir = (px - ex) >= 0f ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, faceDir == 1 ? 180f : 0f, 0f);

        int moveDir = tooClose ? ((ex - px) >= 0f ? 1 : -1) : ((px - ex) >= 0f ? 1 : -1);
        float speed = Mathf.Max(0f, s.moveSpeed);

        Rigidbody2D rb = Rigidbody;
        rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);
    }

    private void TickHover()
    {
        if (state != MirrorState.Move) return;
        if (IsStunned()) return;

        hoverTime += Time.deltaTime;

        float t = hoverPhase + hoverTime;
        float y = Mathf.Sin(t * hoverFrequency) * hoverAmplitude;

        Vector3 pos = hoverBaseLocalPos;
        pos.y += y;

        hoverVisualTransform.localPosition = pos;
    }

    private void TickCooldownAndMaybeStartAttack()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (IsStunned())
        {
            cooldownTimer = 0.1f;
            return;
        }

        PlayerController p = GetPlayer();
        if (p == null)
        {
            cooldownTimer = 0.2f;
            return;
        }

        BeginAttack(p);
    }

    private void BeginAttack(PlayerController player)
    {
        if (projectilePrefab == null) return;
        if (shotsPerAttack <= 0) return;
        if (attackDuration <= 0f) return;

        state = MirrorState.Attack;

        StopHorizontal();

        Vector2 from = transform.position;
        Vector2 to = (Vector2)player.transform.position + (Vector2.up * aimYOffset);
        Vector2 dir = to - from;

        if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
        lockedShotDir = dir.normalized;

        attackTimer = 0f;
        shotInterval = attackDuration / shotsPerAttack;
        if (shotInterval <= 0f) shotInterval = 0.01f;

        shotIndex = 0;

        FireShot();
        shotIndex = 1;
    }

    private void TickAttack()
    {
        StopHorizontal();

        attackTimer += Time.deltaTime;

        while (shotIndex < shotsPerAttack && attackTimer >= shotIndex * shotInterval)
        {
            FireShot();
            shotIndex++;
        }

        if (attackTimer < attackDuration) return;

        EndAttackAndStartCooldown();
    }

    private void EndAttackAndStartCooldown()
    {
        state = MirrorState.Move;
        ResetCooldown();
    }

    private void ResetCooldown()
    {
        cooldownTimer = GetRandomInRange(cooldownRange);
        if (cooldownTimer < 0f) cooldownTimer = 0f;
    }

    private void FireShot()
    {
        MirrorProjectile prefab = projectilePrefab;
        if (prefab == null) return;

        float y = GetRandomInRange(projectileSpawnYRange);

        Vector3 spawnPos = transform.position;
        spawnPos.y += y;

        MirrorProjectile proj = Instantiate(prefab, spawnPos, Quaternion.identity);

        int damage = 0;
        EnemySetting s = Setting;
        if (s != null) damage = Mathf.Max(0, s.attackDamage);

        proj.Initialize(lockedShotDir, damage);
    }

    private float GetRandomInRange(Vector2 range)
    {
        float min = range.x;
        float max = range.y;

        if (max < min)
            (max, min) = (min, max);

        return Random.Range(min, max);
    }

    private bool IsPlayerInTooCloseRange(out PlayerController player)
    {
        player = null;

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = playerLayer,
            useTriggers = true
        };

        int count = tooCloseDetectCollider.Overlap(filter, detectHits);
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

    private PlayerController GetPlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        cachedPlayer = PlayerController.Instance;
        return cachedPlayer;
    }

    public override bool ApplyStun(float duration) => false;

    protected override void OnDied()
    {
        state = MirrorState.Dead;
    }
}