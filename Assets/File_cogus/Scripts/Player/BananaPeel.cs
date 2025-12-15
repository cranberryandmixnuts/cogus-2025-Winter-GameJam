using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class BananaPeel : MonoBehaviour
{
    private Rigidbody2D rb;

    private GameObject owner;
    private float stopSpeed;
    private float stunDuration;

    private bool stopped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(GameObject owner, int direction, float speed, float linearDrag, float stopSpeed, float stunDuration)
    {
        this.owner = owner;
        this.stopSpeed = Mathf.Max(0f, stopSpeed);
        this.stunDuration = Mathf.Max(0f, stunDuration);

        rb.linearDamping = Mathf.Max(0f, linearDrag);

        int dir = direction >= 0 ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * Mathf.Max(0f, speed), rb.linearVelocity.y);
    }

    private void FixedUpdate()
    {
        if (stopped)
            return;

        float vx = Mathf.Abs(rb.linearVelocity.x);
        if (vx <= stopSpeed)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            stopped = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.root.gameObject == owner)
            return;

        IStunnable stunnable = other.GetComponentInParent<IStunnable>();
        if (stunnable != null)
        {
            stunnable.ApplyStun(stunDuration);
            Destroy(gameObject);
        }
    }
}
