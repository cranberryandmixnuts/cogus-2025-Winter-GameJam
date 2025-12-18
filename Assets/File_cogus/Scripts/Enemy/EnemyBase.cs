using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable, IStunnable
{
    [Header("Stats")]
    [SerializeField] private EnemySetting setting;

    [Header("Death")]
    [SerializeField] private float deathDestroyDelay = 3f;
    [SerializeField] private float deathUpImpulse = 3f;

    private Rigidbody2D rb;

    public int currentHealth;
    private float stunTimer;
    private bool dead;
    private bool rewardGiven;

    public EnemySetting Setting => setting;
    public Rigidbody2D Rigidbody => rb;

    public int CurrentHealth => currentHealth;
    public bool IsDead => dead;

    protected float StunRemaining
    {
        get { return stunTimer; }
    }

    protected virtual bool IsInvincible
    {
        get { return false; }
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        currentHealth = GetMaxHealth();
        if (currentHealth < 1) currentHealth = 1;
    }

    protected virtual void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        if (stunTimer < 0f) stunTimer = 0f;
    }

    public int GetMaxHealth()
    {
        if (setting == null) return 0;
        return Mathf.Max(0, setting.maxHealth);
    }

    public virtual void ApplyDamage(int damage)
    {
        if (dead) return;
        if (IsInvincible) return;
        if (damage <= 0) return;
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        if (currentHealth > 0) return;

        currentHealth = 0;
        Die();
    }

    public virtual bool ApplyStun(float duration)
    {
        if (dead) return false;
        if (IsInvincible) return false;
        if (duration <= 0f) return false;

        if (stunTimer < duration)
        {
            stunTimer = duration;
            return true;
        }

        return false;
    }

    protected bool IsStunned()
    {
        return stunTimer > 0f;
    }

    protected void StopHorizontal()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected void StopAllMotion()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    protected virtual void Die()
    {
        if (dead) return;
        dead = true;

        GiveKillRewardOnce();

        StopAllMotion();

        DisableAllColliders();

        AddDeathImpulse();

        if (deathDestroyDelay < 0f) deathDestroyDelay = 0f;
        Destroy(gameObject, deathDestroyDelay);

        OnDied();
    }

    protected virtual void OnDied()
    {
    }

    protected virtual void DisableAllColliders()
    {
        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            cols[i].enabled = false;
        }
    }

    protected virtual void AddDeathImpulse()
    {
        if (rb == null) return;
        if (deathUpImpulse <= 0f) return;

        rb.AddForce(Vector2.up * deathUpImpulse, ForceMode2D.Impulse);
    }

    private void GiveKillRewardOnce()
    {
        if (rewardGiven) return;
        rewardGiven = true;

        if (this.setting.eggDamageIncreaseOnKill <= 0) return;

        PlayerController player = PlayerController.Instance;

        PlayerSetting setting = player.Setting;

        setting.extraEggProjectileDamage += this.setting.eggDamageIncreaseOnKill;
    }
}