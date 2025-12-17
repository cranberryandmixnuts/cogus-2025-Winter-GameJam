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

    [Header("Colliders")]
    [SerializeField] private Collider2D detectRangeCollider;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    private readonly Collider2D[] detectHits = new Collider2D[8];
    private readonly Collider2D[] bodyHits = new Collider2D[8];

    private SnailState state;
    private float contactCooldownTimer;
    private PlayerController cachedPlayer;

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

        cachedPlayer = PlayerController.Instance;
        state = SnailState.Crawl;
    }

    protected override void Update()
    {
        base.Update();

        if (contactCooldownTimer > 0f) contactCooldownTimer -= Time.deltaTime;
        if (contactCooldownTimer < 0f) contactCooldownTimer = 0f;

        if (IsDead) return;

        bool inRange = IsPlayerInDetectRange(out PlayerController p);

        if (state == SnailState.Hide)
        {
            if (!inRange) state = SnailState.Crawl;
        }
        else if (state == SnailState.Crawl)
        {
            if (inRange) state = SnailState.Hide;
        }

        if (p != null) cachedPlayer = p;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

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

        transform.rotation = Quaternion.Euler(0f, dir == -1 ? 180f : 0f, 0f);
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

            bool hit = p.Hit(Setting.attackDamage, false);
            if (hit) contactCooldownTimer = 0.1f;

            return;
        }
    }
}