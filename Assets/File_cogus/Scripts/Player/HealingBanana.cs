using UnityEngine;

public sealed class HealingBanana : MonoBehaviour
{
    [SerializeField] private Collider2D trigger;

    private float activateDelay;
    private int healAmount;

    private bool active;
    private float timer;

    public void Initialize(float activateDelay, int healAmount)
    {
        this.activateDelay = Mathf.Max(0f, activateDelay);
        this.healAmount = Mathf.Max(0, healAmount);

        active = false;
        timer = 0f;

        if (trigger != null)
            trigger.enabled = true;
    }

    private void Update()
    {
        if (active)
            return;

        timer += Time.deltaTime;
        if (timer >= activateDelay)
            active = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        if (player.Vitals != null && healAmount > 0)
            player.Vitals.ApplyHeal(player.CurrentSkin, healAmount);

        Destroy(gameObject);
    }
}
