using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public sealed class PlayerSettings : ScriptableObject
{
    [Serializable]
    public struct SkinStats
    {
        public PlayerSkin skin;
        public int maxHealth;
        public int currentHealth;
        public float moveSpeed;
    }

    [Header("Skin Stats")]
    public SkinStats[] skinStats;

    [Header("Banana Movement")]
    public float bananaAccelTime = 0.35f;
    public float bananaDecelTime = 1.5f;

    [Header("Jump")]
    public AnimationCurve jumpForceCurve;
    public float maxJumpTime = 0.3f;
    public float maxJumpForce = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Egg Ability")]
    public float eggProjectileSpeed = 18f;
    public float eggProjectileMaxDistance = 9f;
    public int baseEggProjectileDamage = 10;
    public int extraEggProjectileDamage = 0;
    public float eggShootCoolTime = 0.3f;

    [Header("Banana Ability")]
    public float bananaPeelSpeed = 10f;
    public float bananaStunDuration = 3f;
    public float bananaPeelCooldown = 1.5f;
    public int maxBananaPeelCount = 15;
    public float healingBananaCooldown = 10f;
    public float healingBananaActivateDelay = 3f;
    public int healingBananaHealAmount = 10;

    [Header("Snail Ability")]
    public float hideTime = 0.5f;

    [Header("Hit")]
    public float hitInvincibleTime = 1f;

    public bool TryGetSkinStatsIndex(PlayerSkin skin, out int index)
    {
        if (skinStats != null)
        {
            for (int i = 0; i < skinStats.Length; i++)
            {
                if (skinStats[i].skin == skin)
                {
                    index = i;
                    return true;
                }
            }
        }

        index = -1;
        return false;
    }

    public int GetMaxHealth(PlayerSkin skin)
    {
        if (!TryGetSkinStatsIndex(skin, out int idx))
            return 0;

        return Mathf.Max(0, skinStats[idx].maxHealth);
    }

    public int GetCurrentHealth(PlayerSkin skin)
    {
        if (!TryGetSkinStatsIndex(skin, out int idx))
            return 0;

        return Mathf.Max(0, skinStats[idx].currentHealth);
    }

    public bool SetCurrentHealth(PlayerSkin skin, int value)
    {
        if (!TryGetSkinStatsIndex(skin, out int idx))
            return false;

        SkinStats s = skinStats[idx];

        int max = Mathf.Max(0, s.maxHealth);
        int v = Mathf.Clamp(value, 0, max);

        if (s.currentHealth == v)
            return false;

        s.currentHealth = v;
        skinStats[idx] = s;
        return true;
    }

    public bool ResetCurrentHealthToMax(PlayerSkin skin)
    {
        if (!TryGetSkinStatsIndex(skin, out int idx))
            return false;

        SkinStats s = skinStats[idx];

        int max = Mathf.Max(0, s.maxHealth);
        if (s.currentHealth == max)
            return false;

        s.currentHealth = max;
        skinStats[idx] = s;
        return true;
    }

    public void ResetAllStatus()
    {
        for (int i = 0; i < skinStats.Length; i++)
        {
            SkinStats s = skinStats[i];
            s.currentHealth = Mathf.Max(0, s.maxHealth);
            skinStats[i] = s;
        }

        extraEggProjectileDamage = 0;
    }

    public float GetMoveSpeed(PlayerSkin skin)
    {
        if (!TryGetSkinStatsIndex(skin, out int idx))
            return 0f;

        return Mathf.Max(0f, skinStats[idx].moveSpeed);
    }
}