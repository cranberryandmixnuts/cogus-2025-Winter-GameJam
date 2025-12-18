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
        WebShot
    }

    private const string AnimStateIdle = "monster_spider_Idle";
    private const string AnimStateWalk = "monster_spider_Walk";
    private const string AnimStateBackWalk = "monster_spider_BackWalk";
    private const string AnimStateBite = "monster_spider_Bite";
    private const string AnimStatePoison = "monster_spider_Poison";
    private const string AnimStateString = "monster_spider_String";
    private const string AnimStateGrow = "monster_spider_Grow";

    [Header("Pattern")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float patternInterval = 1f;
    [SerializeField] private Image HPbar;
    [SerializeField] private Animator Anim;

    [Header("Dive Bite - Common")]
    [SerializeField] private Transform emergeYAnchor;
    [SerializeField] private float descendDuration = 0.35f;
    [SerializeField] private float ascendDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float diveHitNormalizedTime = 0.15f;
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
    [SerializeField, Range(0f, 1f)] private float fixedHitNormalizedTime = 0.15f;
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
    [SerializeField, Range(0f, 1f)] private float webFireNormalizedTime = 0.15f;
    [SerializeField] private int webBreakPressRequired = 6;

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

        HPbar.fillAmount = (float)CurrentHealth / Setting.maxHealth;
    }

    protected override void OnDied()
    {
        if (patternLoop != null)
            StopCoroutine(patternLoop);

        activeMoveTween?.Kill();

        base.OnDied();
    }

    private void PlayAnim(string stateName)
    {
        Anim.Play(stateName, 0, 0f);
        Anim.Update(0f);
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

        Debug.LogError($"SpiderBoss: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }

    private IEnumerator PatternLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        PlayAnim(AnimStateIdle);

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

        PlayAnim(AnimStateIdle);

        activeMoveTween = transform.DOMoveY(targetY, fixedDescendDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);

        PlayAnim(AnimStateGrow);

        float growLen = GetAnimLength(AnimStateGrow);
        float hitTime = growLen * fixedHitNormalizedTime;
        if (hitTime > 0f)
            yield return new WaitForSeconds(hitTime);

        bool hit = player.TryHit(fixedDamage);
        if (hit)
            player.ApplyPoison(fixedPoisonDuration, fixedPoisonDamagePerSecond);

        float remain = growLen - hitTime;
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        PlayAnim(AnimStateIdle);

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

        PlayAnim(AnimStateIdle);

        activeMoveTween = transform.DOMoveY(biteWorldY, descendDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);

        string attackState = applyPoison ? AnimStatePoison : AnimStateBite;
        PlayAnim(attackState);

        float attackLen = GetAnimLength(attackState);
        float hitTime = attackLen * diveHitNormalizedTime;
        if (hitTime > 0f)
            yield return new WaitForSeconds(hitTime);

        TryHitscan(hitCollider, damage, applyPoison);

        float remain = attackLen - hitTime;
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        PlayAnim(AnimStateIdle);

        activeMoveTween = transform.DOMoveY(startY, ascendDuration).SetEase(Ease.Linear);
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

        Vector3 backPos = castPoint.position;
        backPos.x += teleportOffset.x;
        backPos.y += teleportOffset.y;
        backPos.z = transform.position.z;

        transform.position = backPos;

        PlayAnim(AnimStateWalk);

        Vector3 targetPos = castPoint.position;
        targetPos.z = backPos.z;

        activeMoveTween = transform.DOMove(targetPos, webMoveDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);

        PlayAnim(AnimStateString);

        float stringLen = GetAnimLength(AnimStateString);
        float fireTime = stringLen * webFireNormalizedTime;
        if (fireTime > 0f)
            yield return new WaitForSeconds(fireTime);

        Vector3 firePos = webFirePoint.position;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)firePos).normalized;

        SpiderWebProjectile proj = Instantiate(webProjectilePrefab, firePos, Quaternion.identity);
        proj.Initialize(dir, webBreakPressRequired);

        float remain = stringLen - fireTime;
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        PlayAnim(AnimStateBackWalk);

        activeMoveTween = transform.DOMove(backPos, webMoveDuration).SetEase(Ease.Linear);
        yield return WaitUntilTweenEnds(activeMoveTween);

        transform.rotation = baseRotation;
        PlayAnim(AnimStateIdle);
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
            PlayerController player = hitscanResults[i].GetComponent<PlayerController>();
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