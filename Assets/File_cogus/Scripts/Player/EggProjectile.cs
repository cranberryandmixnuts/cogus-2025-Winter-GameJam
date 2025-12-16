using UnityEngine;

public sealed class EggProjectile : MonoBehaviour
{
    private const string BreakAnimStateName = "계란깨지는거";

    [Header("Spin")]
    [SerializeField] private float spinSpeedDegPerSec = 720f;

    [Header("Break")]
    [SerializeField] private Animator anim;
    [SerializeField] private float breakLifetime = 0.3f;

    private Rigidbody2D rb;
    private CircleCollider2D col;

    private GameObject owner;
    private Vector2 startPos;
    private Vector2 moveDir;
    private int damage;
    private float speed;
    private float maxDistance;

    private int spinSign;
    private bool isBreaking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        startPos = transform.position;
        spinSign = Random.value < 0.5f ? -1 : 1;
    }

    public void Initialize(GameObject owner, Vector2 direction, float speed, float maxDistance, int damage)
    {
        this.owner = owner;

        if (direction.sqrMagnitude <= 0.0001f) moveDir = Vector2.right;
        else moveDir = direction.normalized;

        this.speed = Mathf.Max(0f, speed);
        this.maxDistance = Mathf.Max(0f, maxDistance);
        this.damage = Mathf.Max(0, damage);

        if (breakLifetime < 0f)
            breakLifetime = 0f;

        isBreaking = false;

        rb.gravityScale = 0f;
        rb.simulated = true;
        rb.linearVelocity = moveDir * this.speed;

        col.enabled = true;

        startPos = transform.position;
        spinSign = Random.value < 0.5f ? -1 : 1;
    }

    private void Update()
    {
        if (isBreaking) return;

        gameObject.transform.Rotate(0f, 0f, spinSpeedDegPerSec * spinSign * Time.deltaTime);

        if (maxDistance <= 0f) return;

        float dist = ((Vector2)transform.position - startPos).magnitude;
        if (dist >= maxDistance)
            Break();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBreaking) return;
        if (other.transform.root.gameObject == owner) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.ApplyDamage(damage);
            Break();
        }
    }

    public void Break()
    {
        isBreaking = true;

        Destroy(gameObject, breakLifetime);
        anim.Play(BreakAnimStateName, 0, 0f);

        Destroy(col);
        Destroy(rb);
        Destroy(this);
    }
}