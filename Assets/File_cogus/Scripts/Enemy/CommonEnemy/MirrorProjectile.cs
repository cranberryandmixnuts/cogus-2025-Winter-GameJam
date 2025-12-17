using DG.Tweening;
using UnityEngine;

public sealed class MirrorProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 9f;
    [SerializeField] private float maxTravelDistance = 9f;

    [Header("Hit Layers")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    private const float GroundFadeOutDuration = 0.3f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer Sprite;

    private Vector2 dir;
    private int damage;
    private bool initialized;
    private bool Broken;
    private Vector2 startPos;

    public void Initialize(Vector2 direction, int damage)
    {
        dir = direction.sqrMagnitude <= 0.0001f ? Vector2.right : direction.normalized;
        this.damage = Mathf.Max(0, damage);

        initialized = true;
        startPos = transform.position;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        Sprite = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (!initialized) return;
        if (Broken) return;

        float s = Mathf.Max(0f, speed);
        rb.linearVelocity = dir * s;

        float maxDist = Mathf.Max(0f, maxTravelDistance);
        if (maxDist <= 0f) return;

        Vector2 now = transform.position;
        if ((now - startPos).sqrMagnitude >= maxDist * maxDist)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;
        if (Broken) return;

        int otherLayer = other.gameObject.layer;

        if (((1 << otherLayer) & playerLayer.value) != 0)
        {
            PlayerController p = other.GetComponentInParent<PlayerController>();
            if (p != null) p.Hit(damage, true);

            Destroy(gameObject);
            return;
        }

        if (((1 << otherLayer) & groundLayer.value) != 0)
        {
            BeginGroundFadeOutAndDestroy();
            return;
        }
    }

    private void BeginGroundFadeOutAndDestroy()
    {
        if (Broken) return;
        Broken = true;

        Destroy(rb);
        Destroy(col);

        Sprite.DOFade(0f, GroundFadeOutDuration)
        .OnComplete(() => Destroy(gameObject));

        enabled = false;
    }
}