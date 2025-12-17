public interface IDamageable
{
    public void ApplyDamage(int damage);
}

public interface IStunnable
{
    public bool ApplyStun(float duration);
}
