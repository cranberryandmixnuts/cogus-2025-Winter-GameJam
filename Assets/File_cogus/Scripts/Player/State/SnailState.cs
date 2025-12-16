using UnityEngine;

public sealed class SnailState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Snail;

    private enum HidePhase
    {
        None,
        Startup,
        Hidden,
        Recovery,
    }

    private HidePhase phase;
    private float phaseTimer;

    public SnailState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump();
        player.ConsumeJumpBuffer();

        phase = HidePhase.None;
        phaseTimer = 0f;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);
    }

    public override void Exit()
    {
        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        phase = HidePhase.None;
        phaseTimer = 0f;
    }

    public override void Update()
    {
        float hideTime = player.Settings.hideTime;

        if (phase == HidePhase.None)
        {
            if (player.SpecialAbilitiesDown && player.IsGround)
                BeginStartup(hideTime);

            return;
        }

        if (phase == HidePhase.Startup)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer > 0f)
                return;

            if (!player.IsGround)
            {
                phase = HidePhase.None;
                return;
            }

            BeginHidden(hideTime);
            return;
        }

        if (phase == HidePhase.Hidden)
        {
            if (!player.IsGround || player.SpecialAbilitiesUp || !player.SpecialAbilitiesHeld)
                BeginRecovery(hideTime);

            return;
        }

        if (phase == HidePhase.Recovery)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer > 0f)
                return;

            phase = HidePhase.None;
        }
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

    private void BeginStartup(float hideTime)
    {
        phase = HidePhase.Startup;
        phaseTimer = hideTime;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        player.LockMovement(hideTime);
        player.LockSkinChange(hideTime);

        player.StopAllMotion();
    }

    private void BeginHidden(float hideTime)
    {
        phase = HidePhase.Hidden;

        player.SetSnailHidden(true);
        player.SetForcedSkinChangeLock(true);

        player.StopAllMotion();

        if (!player.SpecialAbilitiesHeld || !player.IsGround)
            BeginRecovery(hideTime);
    }

    private void BeginRecovery(float hideTime)
    {
        if (phase == HidePhase.Recovery)
            return;

        phase = HidePhase.Recovery;
        phaseTimer = hideTime;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        player.LockMovement(hideTime);
        player.LockSkinChange(hideTime);

        player.StopAllMotion();
    }
}
