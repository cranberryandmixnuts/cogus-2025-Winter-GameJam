using UnityEngine;

[CreateAssetMenu(menuName = "Game/Boss Setting", fileName = "BossSetting")]
public sealed class BossSetting : ScriptableObject
{
    [Header("Poison")]
    public float poisonDuration = 6f;
    public int poisonDamagePerTick = 2;
    public float poisonTickInterval = 1f;
    public Color poisonTint = new(0.65f, 0.15f, 1f, 1f);

    [Header("Web Bind")]
    public int webBreakPressCount = 6;
}