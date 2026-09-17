using Godot;

namespace ValeDasFlores;

public partial class Farm
{
    public bool UsingGamepad { get; private set; }
    private int _activePad = -1;
    public string Prompt(string keyboard, string controller) => UsingGamepad ? controller : keyboard;

    private static void BindPad(string action, JoyButton button)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action, .22f);
        using var ev = new InputEventJoypadButton { Device = -1, ButtonIndex = button };
        if (!InputMap.ActionHasEvent(action, ev)) InputMap.ActionAddEvent(action, ev);
    }
    private static void BindAxis(string action, JoyAxis axis, float value)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action, .22f);
        InputMap.ActionSetDeadzone(action, .22f);
        using var ev = new InputEventJoypadMotion { Device = -1, Axis = axis, AxisValue = value };
        if (!InputMap.ActionHasEvent(action, ev)) InputMap.ActionAddEvent(action, ev);
    }
    public static float VehicleThrottle() => Input.GetAxis("reverse", "accelerate");
    public static bool VehicleBrake() => Input.IsActionPressed("brake");
    private void RegisterGamepad()
    {
        BindAxis("left", JoyAxis.LeftX, -1); BindAxis("right", JoyAxis.LeftX, 1);
        BindAxis("forward", JoyAxis.LeftY, -1); BindAxis("back", JoyAxis.LeftY, 1);
        BindAxis("look_left", JoyAxis.RightX, -1); BindAxis("look_right", JoyAxis.RightX, 1);
        BindAxis("look_up", JoyAxis.RightY, -1); BindAxis("look_down", JoyAxis.RightY, 1);
        BindPad("accelerate", JoyButton.A); BindPad("reverse", JoyButton.B);
        BindAxis("brake", JoyAxis.TriggerLeft, 1);
        BindPad("jump", JoyButton.A); BindPad("run", JoyButton.LeftStick);
        BindPad("interact", JoyButton.Y); BindPad("context", JoyButton.X);
        BindPad("roof", JoyButton.DpadDown);
        BindPad("camera", JoyButton.RightStick); BindPad("lights", JoyButton.DpadUp);
        BindPad("pause", JoyButton.Start); BindPad("quick_save", JoyButton.Back);
        BindPad("cycle_tool", JoyButton.LeftShoulder); BindPad("lower_tool", JoyButton.RightShoulder);
        BindPad("ui_accept", JoyButton.A); BindPad("ui_cancel", JoyButton.B);
        BindPad("ui_up", JoyButton.DpadUp); BindPad("ui_down", JoyButton.DpadDown);
        BindPad("ui_left", JoyButton.DpadLeft); BindPad("ui_right", JoyButton.DpadRight);
        BindAxis("ui_left", JoyAxis.LeftX, -1); BindAxis("ui_right", JoyAxis.LeftX, 1);
        BindAxis("ui_up", JoyAxis.LeftY, -1); BindAxis("ui_down", JoyAxis.LeftY, 1);
        Input.JoyConnectionChanged += OnPadConnection;
    }
    private void OnPadConnection(long device, bool connected)
    {
        if (!connected && _activePad == device)
        {
            UsingGamepad = false; _activePad = -1;
            RefreshInputHints();
            SetPaused(true);
            _menuMessage.Text = "Controle desconectado. Reconecte ou use teclado e mouse.";
        }
    }
    public override void _ExitTree() => Input.JoyConnectionChanged -= OnPadConnection;

    private bool HasVisibleDialog()
    {
        foreach (var child in GetChildren()) if (child is Window window && window.Visible) return true;
        return false;
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventJoypadButton pad && pad.Pressed ||
            ev is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > .3f)
        { UsingGamepad = true; _activePad = ev.Device; }
        else if (ev is InputEventKey key && key.Pressed || ev is InputEventMouseButton mouse && mouse.Pressed ||
                 ev is InputEventMouseMotion move && move.Relative.Length() > 2)
            UsingGamepad = false;
        RefreshInputHints();
        // Let modal dialogs consume accept/cancel before the pause menu.
        if (_paused && GetViewport().GuiGetFocusOwner() != null &&
            (ev.IsActionPressed("pause") || ev.IsActionPressed("ui_cancel")))
        {
            if (HasVisibleDialog()) return;
            if (!BackMenu()) SetPaused(false); GetViewport().SetInputAsHandled();
        }
    }
    private void LookWithGamepad(float dt)
    {
        Vector2 look = Input.GetVector("look_left", "look_right", "look_up", "look_down", .22f);
        if (Player.Driving)
        {
            _orbitYaw -= look.X * dt * 2.3f;
            _orbitPitch = Mathf.Clamp(_orbitPitch - look.Y * dt * 1.7f, -1.05f, .45f);
        }
        else
        {
            Player.Yaw -= look.X * dt * 2.3f;
            Player.Pitch = Mathf.Clamp(Player.Pitch - look.Y * dt * 1.7f, -1.35f, 1.2f);
        }
    }
    public void ContextAction()
    {
        switch (Activities.NearbyService())
        {
            case "shop": OpenShop(); break;
            case "fuel": Activities.Refuel(); break;
            case "fish": Activities.FishAction(); break;
            case "crop": Activities.CropAction(); break;
        }
    }
}
