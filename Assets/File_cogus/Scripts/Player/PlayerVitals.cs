using UnityEngine;

public sealed class PlayerVitals : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    [SerializeField] private bool initializeCurrentHPOnStart = false;

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
        if (initializeCurrentHPOnStart)
            settings.ResetAllCurrentHealthToMax();
    }

    private void Update()
    {
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (invincibleTimer < 0f) invincibleTimer = 0f;
    }

    public int GetHealth(PlayerSkin skin)
    {
        if (settings == null) return 0;
        return settings.GetCurrentHealth(skin);
    }

    public int GetMaxHealth(PlayerSkin skin)
    {
        if (settings == null) return 0;
        return settings.GetMaxHealth(skin);
    }

    public bool HasAnyAliveSkin()
    {
        if (settings == null) return false;

        int count = System.Enum.GetValues(typeof(PlayerSkin)).Length;

        for (int i = 0; i < count; i++)
        {
            PlayerSkin skin = (PlayerSkin)i;
            if (settings.GetCurrentHealth(skin) > 0)
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
        if (settings == null) return false;
        if (!ignoreInvincible && IsInvincible) return false;
        if (damage <= 0) return false;

        int hp = settings.GetCurrentHealth(skin);
        if (hp <= 0) return false;

        int next = hp - damage;
        if (next < 0) next = 0;

        bool changed = settings.SetCurrentHealth(skin, next);

        if (changed && settings.hitInvincibleTime > 0f)
            SetInvincibleTimer(settings.hitInvincibleTime);

        return changed;
    }

    public bool ApplyHeal(PlayerSkin skin, int amount)
    {
        if (settings == null) return false;
        if (amount <= 0) return false;

        int max = settings.GetMaxHealth(skin);
        if (max <= 0) return false;

        int hp = settings.GetCurrentHealth(skin);
        if (hp >= max) return false;

        int next = hp + amount;
        if (next > max) next = max;

        return settings.SetCurrentHealth(skin, next);
    }
}