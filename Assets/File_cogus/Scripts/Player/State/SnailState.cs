public sealed class SnailState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Snail;

    public SnailState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump();
        player.ConsumeJumpBuffer();
    }

    public override void Exit()
    {
        player.SetSnailHidden(false);
    }

    public override void Update()
    {
        player.SetSnailHidden(player.SpecialAbilitiesHeld);
    }

    public override void FixedUpdate()
    {
        if (player.IsSnailHidden)
        {
            player.StopAllMotion();
            return;
        }

        float speed = player.Settings != null ? player.Settings.GetMoveSpeed(PlayerSkin.Snail) : 0f;
        player.HandleMove(speed);
    }
}
