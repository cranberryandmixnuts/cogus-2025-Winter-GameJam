using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EggProjectile : MonoBehaviour
{
    private Rigidbody2D rb;

    private GameObject owner;
    private Vector2 startPos;
    private int damage;
    private float speed;
    private float maxDistance;
    private int direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    public void Initialize(GameObject owner, int direction, float speed, float maxDistance, int damage)
    {
        this.owner = owner;
        this.direction = direction >= 0 ? 1 : -1;
        this.speed = Mathf.Max(0f, speed);
        this.maxDistance = Mathf.Max(0f, maxDistance);
        this.damage = Mathf.Max(0, damage);

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(this.direction * this.speed, 0f);

        startPos = transform.position;
    }

    private void Update()
    {
        float dist = ((Vector2)transform.position - startPos).magnitude;
        if (dist >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.root.gameObject == owner)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && damage > 0)
            damageable.ApplyDamage(damage);

        Destroy(gameObject);
    }
}
