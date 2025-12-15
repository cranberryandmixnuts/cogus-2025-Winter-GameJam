using UnityEngine;

public sealed class PlayerStateMachine
{
    private PlayerState currentState;

    public PlayerStateType CurrentStateType
    {
        get;
        private set;
    } = PlayerStateType.Missing;

    public void Initialize(PlayerState startState)
    {
        currentState = startState;
        CurrentStateType = startState.StateType;

        Debug.Log("[PlayerFSM] Initialize -> " + CurrentStateType);

        currentState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        PlayerStateType prev = CurrentStateType;

        currentState?.Exit();

        currentState = newState ?? throw new System.ArgumentNullException(nameof(newState));
        CurrentStateType = newState.StateType;

        Debug.Log("[PlayerFSM] " + prev + " -> " + CurrentStateType);

        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
}