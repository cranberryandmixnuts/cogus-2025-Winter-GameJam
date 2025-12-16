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
        player.Anim.Play(PlayerController.AnimEggIdle);
    }

    public override void Update()
    {
        if (player.SpecialAbilitiesDown)
            player.TryFireEggProjectile(player.Settings != null ? player.Settings.eggShootCoolTime : 0f);
    }

    public override void FixedUpdate()
    {
        float speed = player.Settings.GetMoveSpeed(PlayerSkin.Egg);
        player.HandleMove(speed);

        if (!player.IsEggShootLocked)
            player.UpdateMoveAnim(PlayerController.AnimEggIdle, PlayerController.AnimEggWalk);
    }
}