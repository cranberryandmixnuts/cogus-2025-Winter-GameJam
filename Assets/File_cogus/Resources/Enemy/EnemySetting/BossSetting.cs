using UnityEngine;

[CreateAssetMenu(fileName = "BossSetting", menuName = "Scriptable Objects/BossSetting")]
public sealed class BossSetting : ScriptableObject
{
    [Header("Hit Scan")]
    public LayerMask playerLayer;

    [Header("Bite Pattern Common")]
    public float biteDownDuration = 0.25f;
    public float biteHoldTime = 0.05f;
    public float biteUpDuration = 0.25f;

    [Header("Normal Bite")]
    public int normalBiteDamage = 1;

    [Header("Poison Bite")]
    public int poisonBiteDamage = 1;
    public float poisonDuration = 5f;
    public int poisonDamagePerSecond = 1;

    [Header("Web Bind")]
    public int webBindRequiredPressCount = 8;
}   