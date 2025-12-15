using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class BananaPeel : MonoBehaviour
{
    private Rigidbody2D rb;

    private GameObject owner;
    private float stunDuration;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(GameObject owner, int direction, float speed, float stunDuration)
    {
        this.owner = owner;
        this.stunDuration = stunDuration;

        int dir = direction >= 0 ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.gameObject == owner) return;

        IStunnable stunnable = other.GetComponentInParent<IStunnable>();
        if (stunnable == null) return;

        stunnable.ApplyStun(stunDuration);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner.TryGetComponent<PlayerController>(out var player))
            player.NotifyBananaPeelDestroyed();
    }
}