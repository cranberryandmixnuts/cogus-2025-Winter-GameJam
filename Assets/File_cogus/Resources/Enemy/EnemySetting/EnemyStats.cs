using UnityEngine;

[CreateAssetMenu(fileName = "EnemySetting", menuName = "Scriptable Objects/EnemySetting")]
public sealed class EnemyStats : ScriptableObject
{
    public float moveSpeed;
    public int attackDamage;
    public int maxHealth;
    public int eggDamageIncreaseOnKill;
}