using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class SpiderWebProjectile : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 14f;

    [Header("Hit")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite stuckSprite;
    [SerializeField] private Vector2 stuckLocalOffset = Vector2.zero;

    private Rigidbody2D rb;
    private Collider2D col;

    private bool stuck;
    private PlayerController stuckPlayer;
    private int breakPressRequired;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        col.isTrigger = true;
    }

    public void Initialize(Vector2 direction, int breakPressRequired)
    {
        this.breakPressRequired = breakPressRequired;

        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        rb.linearVelocity = dir * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (stuck)
            return;

        int otherLayerMask = 1 << other.gameObject.layer;

        if ((otherLayerMask & groundMask.value) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if ((otherLayerMask & playerMask.value) == 0)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        player.ApplyWebBind(breakPressRequired);

        stuck = true;
        stuckPlayer = player;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        col.enabled = false;

        if (spriteRenderer != null && stuckSprite != null)
            spriteRenderer.sprite = stuckSprite;

        transform.SetParent(player.transform, true);

        Vector3 local = transform.localPosition;
        transform.localPosition = new Vector3(stuckLocalOffset.x, stuckLocalOffset.y, local.z);

        StartCoroutine(WaitUntilUnboundThenDestroy());
    }

    private IEnumerator WaitUntilUnboundThenDestroy()
    {
        while (stuckPlayer != null && stuckPlayer.IsWebBound)
            yield return null;

        Destroy(gameObject);
    }
}