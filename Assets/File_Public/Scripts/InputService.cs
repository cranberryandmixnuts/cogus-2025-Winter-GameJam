using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputService : MonoBehaviour
{
    public static InputService Instance { get; private set; }

    [SerializeField]
    private InputActionAsset actions;

    [SerializeField]
    private string playerMapName = "Player";

    [Header("Player Actions")]
    [SerializeField]
    private string moveActionName = "Move";

    [SerializeField]
    private string jumpActionName = "Jump";

    [SerializeField]
    private string specialAbilitiesActionName = "SpecialAbilities";

    [SerializeField]
    private string skinChangeLeftActionName = "SkinChangeLeft";

    [SerializeField]
    private string skinChangeRightActionName = "SkinChangeRight";

    [SerializeField]
    private string healingBananaThrowActionName = "HealingBananaThrow";

    [SerializeField]
    private string pauseActionName = "Pause";

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction specialAbilitiesAction;
    private InputAction skinChangeLeftAction;
    private InputAction skinChangeRightAction;
    private InputAction healingBananaThrowAction;
    private InputAction pauseAction;

    private InputActionRebindingExtensions.RebindingOperation currentRebind;

    private const string RebindsKey = "InputService_Rebinds";

    public Vector2 Move
    {
        get;
        private set;
    }

    public float MoveAxis
    {
        get;
        private set;
    }

    public bool JumpDown
    {
        get;
        private set;
    }

    public bool JumpUp
    {
        get;
        private set;
    }

    public bool JumpHeld
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesDown
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesUp
    {
        get;
        private set;
    }

    public bool SpecialAbilitiesHeld
    {
        get;
        private set;
    }

    public bool SkinChangeLeftDown
    {
        get;
        private set;
    }

    public bool SkinChangeRightDown
    {
        get;
        private set;
    }

    public bool HealingBananaThrowDown
    {
        get;
        private set;
    }

    public bool PauseDown
    {
        get;
        private set;
    }

    public InputActionAsset Actions
    {
        get { return actions; }
    }

    public event Action OnRebindStarted;
    public event Action OnRebindCompleted;
    public event Action OnRebindCanceled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeActions();
        LoadBindingOverrides();
    }

    private void OnEnable()
    {
        EnableActions(true);
    }

    private void OnDisable()
    {
        EnableActions(false);
    }

    private void Update()
    {
        Move = ReadVector2(moveAction);
        MoveAxis = Mathf.Clamp(Move.x, -1f, 1f);

        JumpDown = WasPressed(jumpAction);
        JumpUp = WasReleased(jumpAction);
        JumpHeld = IsPressed(jumpAction);

        SpecialAbilitiesDown = WasPressed(specialAbilitiesAction);
        SpecialAbilitiesUp = WasReleased(specialAbilitiesAction);
        SpecialAbilitiesHeld = IsPressed(specialAbilitiesAction);

        SkinChangeLeftDown = WasPressed(skinChangeLeftAction);
        SkinChangeRightDown = WasPressed(skinChangeRightAction);

        HealingBananaThrowDown = WasPressed(healingBananaThrowAction);

        PauseDown = WasPressed(pauseAction);
    }

    private void InitializeActions()
    {
        moveAction = FindAction(playerMapName, moveActionName);
        jumpAction = FindAction(playerMapName, jumpActionName);
        specialAbilitiesAction = FindAction(playerMapName, specialAbilitiesActionName);
        skinChangeLeftAction = FindAction(playerMapName, skinChangeLeftActionName);
        skinChangeRightAction = FindAction(playerMapName, skinChangeRightActionName);
        healingBananaThrowAction = FindAction(playerMapName, healingBananaThrowActionName);
        pauseAction = FindAction(playerMapName, pauseActionName);
    }

    private InputAction FindAction(string mapName, string actionName)
    {
        if (actions == null) return null;
        if (string.IsNullOrEmpty(mapName)) return null;
        if (string.IsNullOrEmpty(actionName)) return null;

        string path = mapName + "/" + actionName;
        return actions.FindAction(path, false);
    }

    private void EnableActions(bool enable)
    {
        if (actions == null) return;

        if (enable) actions.Enable();
        else actions.Disable();
    }

    private Vector2 ReadVector2(InputAction action)
    {
        if (action == null) return Vector2.zero;
        return action.ReadValue<Vector2>();
    }

    private bool WasPressed(InputAction action)
    {
        if (action == null) return false;
        return action.WasPressedThisFrame();
    }

    private bool WasReleased(InputAction action)
    {
        if (action == null) return false;
        return action.WasReleasedThisFrame();
    }

    private bool IsPressed(InputAction action)
    {
        if (action == null) return false;
        return action.IsPressed();
    }

    public void SaveBindingOverrides()
    {
        if (actions == null) return;

        string json = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        if (actions == null) return;
        if (!PlayerPrefs.HasKey(RebindsKey)) return;

        string json = PlayerPrefs.GetString(RebindsKey);
        actions.LoadBindingOverridesFromJson(json);
    }

    public void ClearBindingOverrides()
    {
        if (actions == null) return;

        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(RebindsKey);
    }

    public void StartRebind(string mapName, string actionName, int bindingIndex, bool excludeMouse)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null) return;

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return;

        if (currentRebind != null)
            currentRebind.Cancel();

        action.Disable();

        InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex);

        if (excludeMouse)
            operation.WithControlsExcluding("Mouse");

        OnRebindStarted?.Invoke();

        currentRebind = operation
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(o => FinishRebind(action))
            .OnCancel(o => CancelRebind(action));

        currentRebind.Start();
    }

    private void FinishRebind(InputAction action)
    {
        action.Enable();

        currentRebind?.Dispose();
        currentRebind = null;

        SaveBindingOverrides();
        OnRebindCompleted?.Invoke();
    }

    private void CancelRebind(InputAction action)
    {
        action.Enable();

        currentRebind?.Dispose();
        currentRebind = null;

        OnRebindCanceled?.Invoke();
    }
}