using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public sealed class SpiderBoss : EnemyBase
{
    private enum NormalPatternType
    {
        Bite,
        PoisonBite,
        WebShot,
        RageSweep
    }

    [Header("Pattern")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float patternInterval = 1f;
    [SerializeField] private Image HPbar;

    [Header("Dive Bite - Common")]
    [SerializeField] private Transform emergeYAnchor;
    [SerializeField] private float descendDuration = 0.35f;
    [SerializeField] private float ascendDuration = 0.3f;
    [SerializeField] private float biteDelay = 0.05f;
    [SerializeField] private LayerMask playerHitMask;

    [Header("Dive Bite - Normal")]
    [SerializeField] private float normalBiteWorldY = 0f;
    [SerializeField] private Collider2D normalBiteHitCollider;
    [SerializeField] private int normalBiteDamage = 10;

    [Header("Dive Bite - Poison")]
    [SerializeField] private float poisonBiteWorldY = 1f;
    [SerializeField] private Collider2D poisonBiteHitCollider;
    [SerializeField] private int poisonBiteDamage = 8;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private int poisonDamagePerSecond = 1;

    [Header("Fixed Pattern Bite (80/60/40/20%)")]
    [SerializeField] private float fixedDescendDeltaY = 6f;
    [SerializeField] private float fixedDescendDuration = 0.9f;
    [SerializeField] private float fixedAscendDuration = 0.5f;
    [SerializeField] private float fixedHitDelay = 0.05f;
    [SerializeField] private int fixedDamage = 12;
    [SerializeField] private float fixedPoisonDuration = 3f;
    [SerializeField] private int fixedPoisonDamagePerSecond = 1;

    [Header("Web Shot - Positions")]
    [SerializeField] private Transform webTopLeftPoint;
    [SerializeField] private Transform webTopRightPoint;
    [SerializeField] private Vector2 webLeftTeleportOffset = new(-3f, 2f);
    [SerializeField] private Vector2 webRightTeleportOffset = new(3f, 2f);
    [SerializeField] private float webMoveDuration = 0.4f;

    [Header("Web Shot - Fire")]
    [SerializeField] private Transform webFirePoint;
    [SerializeField] private SpiderWebProjectile webProjectilePrefab;
    [SerializeField] private float webFireDelay = 0.05f;
    [SerializeField] private float webAfterFireDelay = 0.1f;
    [SerializeField] private int webBreakPressRequired = 6;

    [Header("Rage Sweep (<= 50%)")]
    [SerializeField] private Transform rageLeftPoint;
    [SerializeField] private Transform rageRightPoint;
    [SerializeField] private GameObject rageWarningObject;
    [SerializeField] private Collider2D rageHitCollider;
    [SerializeField] private float rageTelegraphDuration = 0.8f;
    [SerializeField] private float rageSweepDuration = 0.7f;
    [SerializeField] private float rageBetweenSweepsDelay = 0.05f;
    [SerializeField] private int rageSweepDamage = 14;

    private readonly Queue<int> fixedPatternQueue = new();
    private readonly List<NormalPatternType> normalPatternPool = new();
    private readonly Collider2D[] hitscanResults = new Collider2D[16];

    private Tween activeMoveTween;
    private Coroutine patternLoop;
    private int lastNormalPatternIndex = -1;

    private Quaternion baseRotation;

    private static readonly float[] FixedPatternThresholds =
    {
        0.8f,
        0.6f,
        0.4f,
        0.2f
    };

    private int nextFixedThresholdIndex;
    private bool extraPatternUnlocked;
    private bool extraPatternAdded;

    protected override void Awake()
    {
        base.Awake();

        baseRotation = transform.rotation;

        if (Rigidbody != null)
        {
            Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            Rigidbody.gravityScale = 0f;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
        }

        if (rageWarningObject != null)
            rageWarningObject.SetActive(false);

        if (rageHitCollider != null)
            rageHitCollider.enabled = false;

        normalPatternPool.Add(NormalPatternType.Bite);
        normalPatternPool.Add(NormalPatternType.PoisonBite);
        normalPatternPool.Add(NormalPatternType.WebShot);
    }

    protected override void Start()
    {
        base.Start();
        patternLoop = StartCoroutine(PatternLoop());
    }

    public override bool ApplyStun(float duration)
    {
        return true;
    }

    public override void ApplyDamage(int damage)
    {
        int before = CurrentHealth;

        base.ApplyDamage(damage);

        int after = CurrentHealth;
        if (after == before)
            return;

        float max = GetMaxHealth();
        float beforePercent = before / max;
        float afterPercent = after / max;

        QueueFixedPatternsIfCrossed(beforePercent, afterPercent);
        UnlockExtraPatternIfNeeded(beforePercent, afterPercent);

        HPbar.fillAmount = (float)CurrentHealth / Setting.maxHealth;
    }

    protected override void OnDied()
    {
        if (patternLoop != null)
            StopCoroutine(patternLoop);

        activeMoveTween?.Kill();

        if (rageWarningObject != null)
            rageWarningObject.SetActive(false);

        if (rageHitCollider != null)
            rageHitCollider.enabled = false;

        base.OnDied();
    }

    private IEnumerator PatternLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (!IsDead)
        {
            if (fixedPatternQueue.Count > 0)
            {
                fixedPatternQueue.Dequeue();
                yield return RunFixedPattern();
            }
            else
            {
                yield return RunRandomNormalPattern();
            }

            if (patternInterval > 0f)
                yield return new WaitForSeconds(patternInterval);
        }
    }

    private void QueueFixedPatternsIfCrossed(float beforePercent, float afterPercent)
    {
        while (nextFixedThresholdIndex < FixedPatternThresholds.Length)
        {
            float t = FixedPatternThresholds[nextFixedThresholdIndex];

            if (beforePercent > t && afterPercent <= t)
            {
                fixedPatternQueue.Enqueue(1);
                nextFixedThresholdIndex++;
                continue;
            }

            break;
        }
    }

    private void UnlockExtraPatternIfNeeded(float beforePercent, float afterPercent)
    {
        if (extraPatternUnlocked)
            return;

        if (beforePercent > 0.5f && afterPercent <= 0.5f)
        {
            extraPatternUnlocked = true;
            AddExtraPatternToPoolIfNeeded();
        }
    }

    private void AddExtraPatternToPoolIfNeeded()
    {
        if (extraPatternAdded)
            return;

        normalPatternPool.Add(NormalPatternType.RageSweep);
        extraPatternAdded = true;
        lastNormalPatternIndex = -1;
    }

    private IEnumerator RunFixedPattern()
    {
        activeMoveTween?.Kill();
        transform.rotation = baseRotation;

        PlayerController player = PlayerController.Instance;
        if (player == null)
            yield break;

        float startY = emergeYAnchor.position.y;

        Vector3 pos = transform.position;
        transform.position = new Vector3(0f, startY, pos.z);

        float targetY = startY - fixedDescendDeltaY;

        activeMoveTween = transform.DOMoveY(targetY, fixedDescendDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);

        if (fixedHitDelay > 0f)
            yield return new WaitForSeconds(fixedHitDelay);

        bool hit = player.TryHit(fixedDamage);
        if (hit)
            player.ApplyPoison(fixedPoisonDuration, fixedPoisonDamagePerSecond);

        activeMoveTween = transform.DOMoveY(startY, fixedAscendDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);
    }

    private IEnumerator RunRandomNormalPattern()
    {
        int idx = PickNextNormalPatternIndex();
        NormalPatternType pat = normalPatternPool[idx];

        if (pat == NormalPatternType.Bite)
            yield return RunDiveBite(normalBiteWorldY, normalBiteHitCollider, normalBiteDamage, false);
        else if (pat == NormalPatternType.PoisonBite)
            yield return RunDiveBite(poisonBiteWorldY, poisonBiteHitCollider, poisonBiteDamage, true);
        else if (pat == NormalPatternType.WebShot)
            yield return RunWebShot();
        else if (pat == NormalPatternType.RageSweep)
            yield return RunRageSweep();
    }

    private int PickNextNormalPatternIndex()
    {
        int count = normalPatternPool.Count;
        if (count <= 1)
            return 0;

        int idx;
        do
            idx = Random.Range(0, count);
        while (idx == lastNormalPatternIndex);

        lastNormalPatternIndex = idx;
        return idx;
    }

    private IEnumerator RunDiveBite(float biteWorldY, Collider2D hitCollider, int damage, bool applyPoison)
    {
        activeMoveTween?.Kill();
        transform.rotation = baseRotation;

        PlayerController player = PlayerController.Instance;
        if (player == null)
            yield break;

        float x = player.transform.position.x;
        float startY = emergeYAnchor.position.y;

        Vector3 pos = transform.position;
        transform.position = new Vector3(x, startY, pos.z);

        activeMoveTween = transform.DOMoveY(biteWorldY, descendDuration);
        yield return WaitUntilTweenEnds(activeMoveTween);

        if (biteDelay > 0f)
            yield return new WaitForSeconds(biteDelay);

        TryHitscan(hitCollider, damage, applyPoison);

        activeMoveTween = transform.DOMoveY(startY, ascendDuration);
        yield return WaitUntilTweenEnds(activeMoveTween);
    }

    private IEnumerator RunWebShot()
    {
        activeMoveTween?.Kill();

        PlayerController player = PlayerController.Instance;
        if (player == null)
            yield break;

        Transform castPoint = PickClosestWebPoint(player.transform.position);
        Vector2 teleportOffset = castPoint == webTopLeftPoint ? webLeftTeleportOffset : webRightTeleportOffset;

        Quaternion castRot = castPoint == webTopLeftPoint
            ? baseRotation * Quaternion.Euler(0f, 0f, 45f)
            : baseRotation * Quaternion.Euler(0f, 0f, -45f);

        transform.rotation = castRot;

        Vector3 startPos = castPoint.position;
        startPos.x += teleportOffset.x;
        startPos.y += teleportOffset.y;
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        activeMoveTween = transform.DOMove(castPoint.position, webMoveDuration);
        yield return WaitUntilTweenEnds(activeMoveTween);

        if (webFireDelay > 0f)
            yield return new WaitForSeconds(webFireDelay);

        Vector3 firePos = webFirePoint.position;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)firePos).normalized;

        SpiderWebProjectile proj = Instantiate(webProjectilePrefab, firePos, Quaternion.identity);
        proj.Initialize(dir, webBreakPressRequired);

        if (webAfterFireDelay > 0f)
            yield return new WaitForSeconds(webAfterFireDelay);

        transform.rotation = baseRotation;
    }

    private IEnumerator RunRageSweep()
    {
        activeMoveTween?.Kill();
        transform.rotation = baseRotation;

        if (rageLeftPoint == null || rageRightPoint == null || rageHitCollider == null)
            yield break;

        if (rageWarningObject != null)
            rageWarningObject.SetActive(true);

        if (rageTelegraphDuration > 0f)
            yield return new WaitForSeconds(rageTelegraphDuration);

        if (rageWarningObject != null)
            rageWarningObject.SetActive(false);

        rageHitCollider.enabled = true;

        Vector3 left = rageLeftPoint.position;
        Vector3 right = rageRightPoint.position;

        Vector3 pos = transform.position;
        transform.position = new Vector3(left.x, left.y, pos.z);

        yield return MoveAndDamage(right, rageSweepDuration);

        if (rageBetweenSweepsDelay > 0f)
            yield return new WaitForSeconds(rageBetweenSweepsDelay);

        yield return MoveAndDamage(left, rageSweepDuration);

        rageHitCollider.enabled = false;
    }

    private IEnumerator MoveAndDamage(Vector3 target, float duration)
    {
        bool hitThisPass = false;

        Vector3 start = transform.position;
        target.z = start.z;

        activeMoveTween = transform.DOMove(target, duration).SetEase(Ease.Linear);

        while (activeMoveTween.IsActive() && activeMoveTween.IsPlaying())
        {
            if (!hitThisPass)
                hitThisPass = TryHitscanRageSweep();

            yield return null;
        }

        if (!hitThisPass)
            TryHitscanRageSweep();
    }

    private bool TryHitscanRageSweep()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerHitMask);
        filter.useTriggers = true;

        int count = rageHitCollider.Overlap(filter, hitscanResults);
        for (int i = 0; i < count; i++)
        {
            PlayerController player = hitscanResults[i].GetComponentInParent<PlayerController>();
            if (player == null)
                continue;

            player.TryHit(rageSweepDamage);
            return true;
        }

        return false;
    }

    private Transform PickClosestWebPoint(Vector3 playerPos)
    {
        float dL = Vector2.Distance(playerPos, webTopLeftPoint.position);
        float dR = Vector2.Distance(playerPos, webTopRightPoint.position);

        if (dL <= dR)
            return webTopLeftPoint;

        return webTopRightPoint;
    }

    private IEnumerator WaitUntilTweenEnds(Tween tween)
    {
        while (tween.IsActive() && tween.IsPlaying())
            yield return null;
    }

    private bool TryHitscan(Collider2D hitCollider, int damage, bool applyPoison)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerHitMask);
        filter.useTriggers = true;

        int count = hitCollider.Overlap(filter, hitscanResults);
        for (int i = 0; i < count; i++)
        {
            PlayerController player = hitscanResults[i].GetComponentInParent<PlayerController>();
            if (player == null)
                continue;

            bool hit = player.TryHit(damage);
            if (applyPoison && hit)
                player.ApplyPoison(poisonDuration, poisonDamagePerSecond);

            return true;
        }

        return false;
    }
}