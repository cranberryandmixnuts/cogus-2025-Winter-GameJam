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
        public float moveSpeed;

        public int eggProjectileDamage;
    }

    [Header("Skin Stats")]
    public SkinStats[] skinStats;

    [Header("Movement")]
    public float groundAccelTime = 0.25f;
    public float airAccelTime = 0.3f;
    public float airReleaseDecelTime = 0.3f;
    [Range(0f, 1f)] public float startSpeedRatio = 0.15f;

    [Header("Jump")]
    public AnimationCurve jumpForceCurve;
    public float maxJumpTime = 0.3f;
    public float maxJumpForce = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Egg Ability")]
    public float eggProjectileSpeed = 18f;
    public float eggProjectileMaxDistance = 10f;

    [Header("Banana Ability")]
    public float bananaPeelSpeed = 12f;
    public float bananaPeelLinearDrag = 6f;
    public float bananaPeelStopSpeed = 0.2f;
    public float bananaStunDuration = 3f;

    [Header("Healing Banana")]
    public float healingBananaActivateDelay = 3f;
    public int healingBananaHealAmount = 10;

    [Header("Hit")]
    public float hitInvincibleTime = 0.25f;

    public bool TryGetSkinStats(PlayerSkin skin, out SkinStats stats)
    {
        if (skinStats != null)
        {
            for (int i = 0; i < skinStats.Length; i++)
            {
                if (skinStats[i].skin == skin)
                {
                    stats = skinStats[i];
                    return true;
                }
            }
        }

        stats = default;
        return false;
    }

    public int GetMaxHealth(PlayerSkin skin)
    {
        if (TryGetSkinStats(skin, out SkinStats s))
            return Mathf.Max(0, s.maxHealth);

        return 0;
    }

    public float GetMoveSpeed(PlayerSkin skin)
    {
        if (TryGetSkinStats(skin, out SkinStats s))
            return Mathf.Max(0f, s.moveSpeed);

        return 0f;
    }

    public int GetEggDamage(PlayerSkin skin)
    {
        if (TryGetSkinStats(skin, out SkinStats s))
            return Mathf.Max(0, s.eggProjectileDamage);

        return 0;
    }
}