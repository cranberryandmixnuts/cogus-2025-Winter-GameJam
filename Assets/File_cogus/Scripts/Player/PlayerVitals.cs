using UnityEngine;

public sealed class PlayerVitals : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    [SerializeField] private int[] currentHealth;

    private bool forcedInvincible;
    private float invincibleTimer;

    public bool IsInvincible
    {
        get
        {
            if (forcedInvincible) return true;
            return invincibleTimer > 0f;
        }
    }

    private void Awake()
    {
        InitializeFromSettings();
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;

        if (invincibleTimer < 0f)
            invincibleTimer = 0f;
    }

    public void InitializeFromSettings()
    {
        int count = System.Enum.GetValues(typeof(PlayerSkin)).Length;

        if (currentHealth == null || currentHealth.Length != count)
            currentHealth = new int[count];

        for (int i = 0; i < count; i++)
        {
            PlayerSkin skin = (PlayerSkin)i;
            currentHealth[i] = settings != null ? settings.GetMaxHealth(skin) : 0;
        }
    }

    public int GetHealth(PlayerSkin skin)
    {
        int idx = (int)skin;

        if (currentHealth == null) return 0;
        if (idx < 0 || idx >= currentHealth.Length) return 0;

        return currentHealth[idx];
    }

    public int GetMaxHealth(PlayerSkin skin)
    {
        if (settings == null) return 0;
        return settings.GetMaxHealth(skin);
    }

    public bool HasAnyAliveSkin()
    {
        if (currentHealth == null) return false;

        for (int i = 0; i < currentHealth.Length; i++)
        {
            if (currentHealth[i] > 0)
                return true;
        }

        return false;
    }

    public void SetForcedInvincible(bool value)
    {
        forcedInvincible = value;
    }

    public bool SetInvincibleTimer(float time)
    {
        if (time <= 0f) return false;
        if (invincibleTimer > time) return false;

        invincibleTimer = time;
        return true;
    }

    public bool ApplyDamage(PlayerSkin skin, int damage, bool ignoreInvincible)
    {
        if (!ignoreInvincible && IsInvincible) return false;
        if (damage <= 0) return false;

        int idx = (int)skin;

        if (currentHealth == null) return false;
        if (idx < 0 || idx >= currentHealth.Length) return false;
        if (currentHealth[idx] <= 0) return false;

        currentHealth[idx] -= damage;
        if (currentHealth[idx] < 0)
            currentHealth[idx] = 0;

        if (settings != null && settings.hitInvincibleTime > 0f)
            SetInvincibleTimer(settings.hitInvincibleTime);

        return true;
    }

    public bool ApplyHeal(PlayerSkin skin, int amount)
    {
        if (amount <= 0) return false;

        int idx = (int)skin;

        if (currentHealth == null) return false;
        if (idx < 0 || idx >= currentHealth.Length) return false;

        int max = GetMaxHealth(skin);
        if (max <= 0) return false;
        if (currentHealth[idx] >= max) return false;

        currentHealth[idx] += amount;
        if (currentHealth[idx] > max)
            currentHealth[idx] = max;

        return true;
    }
}
