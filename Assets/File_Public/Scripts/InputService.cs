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

    [SerializeField]
    private string moveActionName = "Move";

    [SerializeField]
    private string jumpActionName = "Jump";

    [SerializeField]
    private string dashActionName = "Dash";

    [SerializeField]
    private string parryActionName = "Parry";

    [SerializeField]
    private string healActionName = "Heal";

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction parryAction;
    private InputAction healAction;

    private InputActionRebindingExtensions.RebindingOperation currentRebind;

    private const string RebindsKey = "InputService_Rebinds";

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

    public bool DashDown
    {
        get;
        private set;
    }

    public bool ParryDown
    {
        get;
        private set;
    }

    public bool ParryHeld
    {
        get;
        private set;
    }

    public bool HealHeld
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
        Vector2 move = moveAction.ReadValue<Vector2>();
        MoveAxis = Mathf.Clamp(move.x, -1f, 1f);

        JumpDown = jumpAction.WasPressedThisFrame();
        JumpUp = jumpAction.WasReleasedThisFrame();
        JumpHeld = jumpAction.IsPressed();

        DashDown = dashAction.WasPressedThisFrame();

        ParryDown = parryAction.WasPressedThisFrame();
        ParryHeld = parryAction.IsPressed();

        HealHeld = healAction.IsPressed();
    }

    private void InitializeActions()
    {
        moveAction = FindAction(playerMapName, moveActionName);
        jumpAction = FindAction(playerMapName, jumpActionName);
        dashAction = FindAction(playerMapName, dashActionName);
        parryAction = FindAction(playerMapName, parryActionName);
        healAction = FindAction(playerMapName, healActionName);
    }

    private InputAction FindAction(string mapName, string actionName)
    {
        string path = mapName + "/" + actionName;
        return actions.FindAction(path, false);
    }

    private void EnableActions(bool enable)
    {
        if (enable) actions.Enable();
        else actions.Disable();
    }

    public void SaveBindingOverrides()
    {
        string json = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        if (!PlayerPrefs.HasKey(RebindsKey))
            return;

        string json = PlayerPrefs.GetString(RebindsKey);
        actions.LoadBindingOverridesFromJson(json);
    }

    public void ClearBindingOverrides()
    {
        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(RebindsKey);
    }

    public void StartRebind(string mapName, string actionName, int bindingIndex, bool excludeMouse)
    {
        InputAction action = FindAction(mapName, actionName);

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return;

        currentRebind.Cancel();

        action.Disable();

        var operation = action.PerformInteractiveRebinding(bindingIndex);

        if (excludeMouse)
        {
            operation.WithControlsExcluding("Mouse");
        }

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