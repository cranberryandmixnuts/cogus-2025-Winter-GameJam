public sealed class BananaState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Banana;

    public BananaState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetSnailHidden(false);
        player.Anim.Play(PlayerController.AnimBananaIdle);
    }

    public override void Update()
    {
        if (player.SpecialAbilitiesDown)
            player.TryThrowBananaPeel();

        if (player.HealingBananaThrowDown)
            player.TryDropHealingBanana();

        if (player.HasJumpBuffer && (player.IsGround || player.HasCoyote))
            player.StartJump();

        if (!player.JumpHeld && player.IsJumping)
            player.StopRising();
    }

    public override void FixedUpdate()
    {
        float speed = player.Setting != null ? player.Setting.GetMoveSpeed(PlayerSkin.Banana) : 0f;
        player.HandleMove(speed);
        player.HandleJump();

        if (!player.IsGround)
            player.Anim.Play(PlayerController.AnimBananaJump);
        else
            player.UpdateMoveAnim(PlayerController.AnimBananaIdle, PlayerController.AnimBananaWalk);
    }
}