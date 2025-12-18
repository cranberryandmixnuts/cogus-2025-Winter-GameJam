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
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        player.CancelJump();
        player.ConsumeJumpBuffer();

        phase = HidePhase.None;
        phaseTimer = 0f;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        player.Anim.Play(PlayerController.AnimSnailIdle);
    }

    public override void Exit()
    {
        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        phase = HidePhase.None;
        phaseTimer = 0f;

        player.Anim.Play(PlayerController.AnimSnailIdle);
    }

    public override void Update()
    {
        float hideTime = player.Setting != null ? player.Setting.hideTime : 0f;

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
                player.Anim.Play(PlayerController.AnimSnailIdle);
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
        if (phase != HidePhase.None)
        {
            player.StopAllMotion();
            return;
        }

        if (player.IsSnailHidden)
        {
            player.StopAllMotion();
            return;
        }

        float speed = player.Setting != null ? player.Setting.GetMoveSpeed(PlayerSkin.Snail) : 0f;
        player.HandleMove(speed);
        player.UpdateMoveAnim(PlayerController.AnimSnailIdle, PlayerController.AnimSnailWalk);
    }

    private void BeginStartup(float hideTime)
    {
        phase = HidePhase.Startup;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        player.Anim.Play(PlayerController.AnimSnailEnterShell);

        float animTime = player.GetAnimLength(PlayerController.AnimSnailEnterShell);
        if (animTime > 0f) hideTime = animTime;

        phaseTimer = hideTime;

        player.LockMovement(phaseTimer);
        player.LockSkinChange(phaseTimer);

        player.StopAllMotion();
    }

    private void BeginHidden(float hideTime)
    {
        SoundStorage.Instance.SnailInOut.Play();
        phase = HidePhase.Hidden;

        player.SetSnailHidden(true);
        player.SetForcedSkinChangeLock(true);

        player.StopAllMotion();

        if (!player.SpecialAbilitiesHeld || !player.IsGround)
            BeginRecovery(hideTime);
    }

    private void BeginRecovery(float hideTime)
    {
        SoundStorage.Instance.SnailInOut.Play();
        if (phase == HidePhase.Recovery)
            return;

        phase = HidePhase.Recovery;

        player.SetSnailHidden(false);
        player.SetForcedSkinChangeLock(false);

        player.Anim.Play(PlayerController.AnimSnailExitShell);

        float animTime = player.GetAnimLength(PlayerController.AnimSnailExitShell);
        if (animTime > 0f) hideTime = animTime;

        phaseTimer = hideTime;

        player.LockMovement(phaseTimer);
        player.LockSkinChange(phaseTimer);

        player.StopAllMotion();
    }
}