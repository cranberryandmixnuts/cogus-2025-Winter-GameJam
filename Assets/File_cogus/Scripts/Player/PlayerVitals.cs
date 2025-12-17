using UnityEngine;

public sealed class PlayerVitals : MonoBehaviour
{
    [SerializeField] private PlayerSetting setting;

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

    private void Update()
    {
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (invincibleTimer < 0f) invincibleTimer = 0f;
    }

    public int GetHealth(PlayerSkin skin)
    {
        if (setting == null) return 0;
        return setting.GetCurrentHealth(skin);
    }

    public int GetMaxHealth(PlayerSkin skin)
    {
        if (setting == null) return 0;
        return setting.GetMaxHealth(skin);
    }

    public bool HasAnyAliveSkin()
    {
        if (setting == null) return false;

        int count = System.Enum.GetValues(typeof(PlayerSkin)).Length;

        for (int i = 0; i < count; i++)
        {
            PlayerSkin skin = (PlayerSkin)i;
            if (setting.GetCurrentHealth(skin) > 0)
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
        if (setting == null) return false;
        if (!ignoreInvincible && IsInvincible) return false;
        if (damage <= 0) return false;

        int hp = setting.GetCurrentHealth(skin);
        if (hp <= 0) return false;

        int next = hp - damage;
        if (next < 0) next = 0;

        bool changed = setting.SetCurrentHealth(skin, next);

        if (changed && setting.hitInvincibleTime > 0f)
            SetInvincibleTimer(setting.hitInvincibleTime);

        return changed;
    }

    public bool ApplyHeal(PlayerSkin skin, int amount)
    {
        if (setting == null) return false;
        if (amount <= 0) return false;

        int max = setting.GetMaxHealth(skin);
        if (max <= 0) return false;

        int hp = setting.GetCurrentHealth(skin);
        if (hp >= max) return false;

        int next = hp + amount;
        if (next > max) next = max;

        return setting.SetCurrentHealth(skin, next);
    }
}