using Godot;
using System;

namespace ValeDasFlores;

public partial class Farm : Node3D
{
    public Player Player = null!;
    public Tractor Tractor = null!;
    public Hilux Hilux = null!;
    public Jeep Jeep = null!;
    public bool DrivingJeep => Player.Driving && Jeep.Occupied;
    public bool DrivingHilux => Player.Driving && Hilux.Occupied;
    public bool DrivingTractor => Player.Driving && Tractor.Occupied;
    public CharacterBody3D ActiveVehicle => DrivingJeep ? Jeep : DrivingHilux ? Hilux : Tractor;
    public float VehicleSpeed => DrivingJeep ? Jeep.Speed : DrivingHilux ? Hilux.Speed : Tractor.Speed;
    public string VehicleName => DrivingJeep ? "WILLYS CJ5" : DrivingHilux ? "HILUX 1999" : "CBT 2105";
    public Camera3D Camera = null!;
    public Node3D World = null!;
    public FarmTerrain Terrain = null!;
    public FarmActivities Activities = null!;
    public WorldBuilder Builder = null!;
    private Label _progress = null!;
    private Label _status = null!, _hint = null!, _speed = null!, _toast = null!;
    private Control _pausePanel = null!;
    private bool _paused;
    private float _orbitYaw, _orbitPitch = -.3f, _zoom = 7.7f, _messageTime;
    private bool _vehicleThird = true;
    private bool _snapCamera = true;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        RegisterInput();
        RegisterGamepad();
        World = new Node3D { Name = "FarmWorld", ProcessMode = ProcessModeEnum.Pausable }; AddChild(World);
        FarmWorld.Build(World); Terrain = World.GetNode<FarmTerrain>("Terrain");
        Tractor = new Tractor { Position = new(-4, .15f, 2), ProcessMode = ProcessModeEnum.Pausable }; AddChild(Tractor);
        Hilux = new Hilux { Position = new(34, .2f, 18), Rotation = new(0, Mathf.Pi / 2, 0), ProcessMode = ProcessModeEnum.Pausable }; AddChild(Hilux);
        Jeep = new Jeep { Position = new(40, .2f, 20), Rotation = new(0, Mathf.Pi / 2, 0), ProcessMode = ProcessModeEnum.Pausable }; AddChild(Jeep);
        Player = new Player { Position = new(-10, .05f, 18), Yaw = -.5f, ProcessMode = ProcessModeEnum.Pausable }; AddChild(Player);
        Camera = new Camera3D { Current = true, Fov = 78, Near = .06f, Far = 400 }; AddChild(Camera);
        Activities = new FarmActivities { Farm = this, ProcessMode = ProcessModeEnum.Pausable }; AddChild(Activities);
        Builder = new WorldBuilder { Farm = this }; AddChild(Builder);
        CreateHud();
        _Process(0);
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += () => { SetPaused(true); Confirm("Sair? Alterações após o último save serão perdidas.", () => QuitGame()); };
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--smoke")) AddChild(new SmokeTest { Farm = this });
        else if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--gameplay-test")) AddChild(new GameplayTest { Farm = this });
        else if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--input-test")) AddChild(new InputTest { Farm = this });
        else if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--vehicle-test")) AddChild(new VehicleTest { Farm = this });
        else ShowTitleMenu();
    }

    private static void RegisterInput()
    {
        (string Name, Key Code)[] keys = { ("forward", Key.W), ("back", Key.S), ("left", Key.A), ("right", Key.D),
            ("roof", Key.H), ("accelerate", Key.W), ("reverse", Key.S), ("brake", Key.Space), ("run", Key.Shift), ("jump", Key.Space), ("interact", Key.E), ("camera", Key.V), ("lights", Key.F), ("pause", Key.Escape) };
        foreach (var key in keys)
        {
            if (!InputMap.HasAction(key.Name)) InputMap.AddAction(key.Name);
            InputMap.ActionAddEvent(key.Name, new InputEventKey { PhysicalKeycode = key.Code });
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("pause")) { if (!HasVisibleDialog() && !(_paused && BackMenu())) SetPaused(!_paused); return; }
        if (ev.IsActionPressed("quick_save") && !HasVisibleDialog()) { QuickSave(); return; }
        if (ev is InputEventKey saveKey && saveKey.Pressed && !saveKey.Echo && saveKey.PhysicalKeycode == Key.F5) { QuickSave(); return; }
        if (_paused) return;
        if (ev is InputEventKey builderKey && builderKey.Pressed && !builderKey.Echo && builderKey.PhysicalKeycode == Key.F2) { Builder.Toggle(); return; }
        if (Builder.Active) { Builder.HandleInput(ev); return; }
        if (ev.IsActionPressed("context")) ContextAction();
        if (ev.IsActionPressed("roof")) ChangeJeepRoof();
        if (ev.IsActionPressed("cycle_tool")) SelectTool(Tractor.Tool == 0 ? 1 : Tractor.Tool == 1 && Activities.State.HarrowOwned ? 2 : 0);
        if (ev.IsActionPressed("lower_tool") && DrivingTractor) Tractor.ToggleTool();
        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.PhysicalKeycode)
            {
                case Key.G: Activities.Refuel(); break;
                case Key.B: OpenShop(); break;
                case Key.P: Activities.FishAction(); break;
                case Key.Key1: SelectTool(1); break;
                case Key.Key2: SelectTool(2); break;
                case Key.Key3: SelectTool(0); break;
                case Key.C: Activities.CropAction(); break;
                case Key.R: if (DrivingTractor) Tractor.ToggleTool(); break;
            }
        }
        if (ev is InputEventMouseMotion mouse && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (Player.Driving)
            {
                _orbitYaw -= mouse.Relative.X * .0028f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - mouse.Relative.Y * .0028f, -1.05f, .45f);
            }
            else
            {
                Player.Yaw -= mouse.Relative.X * .0028f;
                Player.Pitch = Mathf.Clamp(Player.Pitch - mouse.Relative.Y * .0028f, -1.35f, 1.2f);
            }
        }
        if (ev is InputEventMouseButton button && button.Pressed)
        {
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (Activities.NearbyService().Length > 0) ContextAction();
                else if (CanEnter()) Interact();
            }
            if (button.ButtonIndex == MouseButton.WheelUp) _zoom = Mathf.Max(4.5f, _zoom - .6f);
            if (button.ButtonIndex == MouseButton.WheelDown) _zoom = Mathf.Min(13, _zoom + .6f);
        }
        if (ev.IsActionPressed("interact")) Interact();
        if (ev.IsActionPressed("camera")) ToggleCamera();
        if (ev.IsActionPressed("lights") && Player.Driving) { if (DrivingJeep) Jeep.ToggleLights(); else if (DrivingHilux) Hilux.ToggleLights(); else Tractor.ToggleLights(); }
    }

    public void ToggleCamera()
    {
        if (Player.Driving) _vehicleThird = !_vehicleThird;
        else Player.ThirdPerson = !Player.ThirdPerson;
        _snapCamera = true;
    }
    public void SnapCamera() { _snapCamera = true; }
    public bool SelectTool(int tool)
    {
        if (!DrivingTractor || Mathf.Abs(Tractor.Speed) > .1f) { Message("Pare o trator para trocar de ferramenta."); return false; }
        if (tool == 2 && !Activities.State.HarrowOwned) { Message("Compre a grade na loja por R$ 220."); return false; }
        Tractor.Equip(tool); Message($"{Tractor.ToolName} selecionada. {Prompt("R", "R1")} para baixar/levantar."); return true;
    }

    public override void _Process(double delta)
    {
        if (_paused) return;
        if (Builder.Active) Builder.Tick((float)delta); else { LookWithGamepad((float)delta); UpdateCamera((float)delta); }
        _status.Text = Player.Driving ? $"NA DIREÇÃO  /  {VehicleName}" : "EXPLORANDO  /  A PÉ";
        _speed.Text = Player.Driving ? $"{Mathf.Abs(VehicleSpeed) * 3.6f:00} km/h\n{(VehicleSpeed < -.1f ? "RÉ" : "FRENTE")}   •   FARÓIS {((DrivingJeep ? Jeep.LightsOn : DrivingHilux ? Hilux.LightsOn : Tractor.LightsOn) ? "ACESOS" : "APAGADOS")}" : "MANHÃ NO CAMPO\nFazenda Vale das Flores";
        _hint.Text = Activities.ContextHint();
        if (!Player.Driving && CanEnter()) _hint.Text += (_hint.Text.Length > 0 ? "\n" : "") + $"{Prompt("E", "△")}  Entrar: {NearbyVehicleName()}";
        else if (Player.Driving && Mathf.Abs(VehicleSpeed) <= .4f && _hint.Text.Length == 0)
            _hint.Text = $"{Prompt("E", "△")}  Descer: {VehicleName}";
        _progress.Text = $"R$ {Activities.State.Money}   /   NÍVEL {Activities.Level}   /   {Activities.State.Xp} XP\n{Activities.Objective}";
        if (DrivingTractor) _speed.Text += $"\nDIESEL {Tractor.Fuel:0.0}/80 L\n{Tractor.ToolName} {(Tractor.ToolLowered ? "BAIXADA" : "LEVANTADA")}";
        if ((DrivingJeep || (!Player.Driving && NearbyVehicle() == Jeep)) && Mathf.Abs(Jeep.Speed) <= .1f)
            _hint.Text += $"\n{Prompt("H", "D-PAD DOWN")}  Capota: {(Jeep.RoofOn ? "retirar" : "colocar")}";
        if (Builder.Active) _hint.Text = Builder.Hint;
        _hint.GetParent<Control>().Visible = _hint.Text.Length > 0;
        _messageTime -= (float)delta;
        _toast.Visible = _messageTime > 0;
    }

    private void UpdateCamera(float dt)
    {
        bool third = Player.Driving ? _vehicleThird : Player.ThirdPerson;
        float yaw = Player.Driving ? ActiveVehicle.Rotation.Y + _orbitYaw : Player.Yaw;
        float pitch = Player.Driving ? _orbitPitch : Player.Pitch;
        var basis = Basis.FromEuler(new(pitch, yaw, 0));
        Vector3 target = Player.Driving ? (DrivingJeep ? Jeep.Seat : DrivingHilux ? Hilux.Seat : Tractor.Seat) : Player.GlobalPosition + Vector3.Up * 1.67f;
        Tractor.Driver.Visible = DrivingTractor && third;
        Hilux.Driver.Visible = DrivingHilux && third;
        Jeep.Driver.Visible = DrivingJeep && third;
        Vector3 position = target;
        if (third)
        {
            var desired = target + basis.Z * (Player.Driving ? _zoom : 4.2f);
            var query = PhysicsRayQueryParameters3D.Create(target, desired);
            query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid(), ActiveVehicle.GetRid() };
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            position = hit.Count > 0 ? hit["position"].AsVector3() + hit["normal"].AsVector3() * .28f : desired;
            position.Y = Mathf.Max(Terrain.HeightAt(position.X, position.Z) + .25f, position.Y);
        }
        Camera.GlobalPosition = _snapCamera || !third ? position : Camera.GlobalPosition.Lerp(position, 1 - Mathf.Exp(-12 * dt));
        Camera.GlobalBasis = basis;
        _snapCamera = false;
    }

    public void Message(string text) { _toast.Text = text; _messageTime = 5; if (_shopOpen && _shopFeedback != null) _shopFeedback.Text = text; }

    public void SetPaused(bool pause)
    {
        _paused = pause; GetTree().Paused = pause; _pausePanel.Visible = pause;
        RefreshMenuPresentation(pause);
        _shopOpen = false; if (_shopPanel != null) _shopPanel.Visible = false;
        if (pause) RefreshSlots();
        else GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        if (_hudGroup != null) _hudGroup.Visible = !pause;
        Input.MouseMode = pause ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && IsNodeReady() && !_paused) SetPaused(true);
    }

    private static Label Text(Control parent, string text, int size, Color color)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label); return label;
    }

    private static PanelContainer Panel(Control parent, Vector2 position, Vector2 size)
    {
        var panel = new PanelContainer { Position = position, Size = size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", RetroBox("24271ee8", "837448", 2, 18));
        parent.AddChild(panel); return panel;
    }

    private void CreateHud() => CreateRetroHud();
}
