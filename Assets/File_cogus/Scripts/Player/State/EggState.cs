public sealed class EggState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Egg;

    public EggState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetSnailHidden(false);
        player.CancelJump();
        player.ConsumeJumpBuffer();
    }

    public override void Update()
    {
        if (player.SpecialAbilitiesDown)
            player.TryFireEggProjectile(player.Settings.eggShootCoolTime);
    }

    public override void FixedUpdate()
    {
        float speed = player.Settings.GetMoveSpeed(PlayerSkin.Egg);
        player.HandleMove(speed);
    }
}
