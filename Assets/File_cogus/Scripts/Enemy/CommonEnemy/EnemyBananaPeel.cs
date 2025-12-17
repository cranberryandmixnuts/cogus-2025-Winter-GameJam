using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemyBananaPeel : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;

    private int damage;
    private bool consumed;

    public void Initialize(int damageValue)
    {
        damage = damageValue;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        if (!other.TryGetComponent<PlayerController>(out var p)) return;

        p.TryHit(damage);

        consumed = true;
        Destroy(gameObject);
    }
}