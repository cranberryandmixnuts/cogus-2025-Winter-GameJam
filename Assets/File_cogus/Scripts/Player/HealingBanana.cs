using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class HealingBanana : MonoBehaviour
{
    private Rigidbody2D rb;

    private float activateDelay;
    private int healAmount;

    private bool active;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(float activateDelay, int healAmount)
    {
        this.activateDelay = activateDelay;
        this.healAmount = healAmount;

        active = false;
        timer = 0f;
    }

    private void Update()
    {
        if (active) return;

        timer += Time.deltaTime;
        if (timer >= activateDelay) active = true;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        if (!other.TryGetComponent<PlayerController>(out var player)) other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        player.Vitals.ApplyHeal(player.CurrentSkin, healAmount);

        Destroy(gameObject);
    }
}