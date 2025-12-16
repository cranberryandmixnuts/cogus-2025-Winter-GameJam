using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class EggProjectile : MonoBehaviour
{
    private Rigidbody2D rb;

    private GameObject owner;
    private Vector2 startPos;
    private Vector2 moveDir;
    private int damage;
    private float speed;
    private float maxDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    public void Initialize(GameObject owner, Vector2 direction, float speed, float maxDistance, int damage)
    {
        this.owner = owner;

        if (direction.sqrMagnitude <= 0.0001f) moveDir = Vector2.right;
        else moveDir = direction.normalized;

        this.speed = Mathf.Max(0f, speed);
        this.maxDistance = Mathf.Max(0f, maxDistance);
        this.damage = Mathf.Max(0, damage);

        rb.gravityScale = 0f;
        rb.linearVelocity = moveDir * this.speed;

        startPos = transform.position;
    }

    private void Update()
    {
        float dist = ((Vector2)transform.position - startPos).magnitude;
        if (dist >= maxDistance) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.gameObject == owner) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.ApplyDamage(damage);
            Destroy(gameObject);
        }
    }
}