using Godot;
using System;

namespace ValeDasFlores;

public partial class Farm : Node3D
{
    public Player Player = null!;
    public Tractor Tractor = null!;
    public Camera3D Camera = null!;
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
        var world = new Node3D { Name = "FarmWorld", ProcessMode = ProcessModeEnum.Pausable }; AddChild(world);
        FarmWorld.Build(world);
        Tractor = new Tractor { Position = new(-4, .15f, 2), ProcessMode = ProcessModeEnum.Pausable }; AddChild(Tractor);
        Player = new Player { Position = new(-.8f, .05f, 5.2f), Yaw = .7f, ProcessMode = ProcessModeEnum.Pausable }; AddChild(Player);
        Camera = new Camera3D { Current = true, Fov = 78, Near = .06f, Far = 400 }; AddChild(Camera);
        CreateHud();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--smoke")) AddChild(new SmokeTest { Farm = this });
    }

    private static void RegisterInput()
    {
        (string Name, Key Code)[] keys = { ("forward", Key.W), ("back", Key.S), ("left", Key.A), ("right", Key.D),
            ("run", Key.Shift), ("jump", Key.Space), ("interact", Key.E), ("camera", Key.V), ("lights", Key.F), ("pause", Key.Escape) };
        foreach (var key in keys)
        {
            if (!InputMap.HasAction(key.Name)) InputMap.AddAction(key.Name);
            InputMap.ActionAddEvent(key.Name, new InputEventKey { PhysicalKeycode = key.Code });
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("pause")) { SetPaused(!_paused); return; }
        if (_paused) return;
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
            if (button.ButtonIndex == MouseButton.WheelUp) _zoom = Mathf.Max(4.5f, _zoom - .6f);
            if (button.ButtonIndex == MouseButton.WheelDown) _zoom = Mathf.Min(13, _zoom + .6f);
        }
        if (ev.IsActionPressed("interact")) Interact();
        if (ev.IsActionPressed("camera")) ToggleCamera();
        if (ev.IsActionPressed("lights") && Player.Driving) Tractor.ToggleLights();
    }

    public void ToggleCamera()
    {
        if (Player.Driving) _vehicleThird = !_vehicleThird;
        else Player.ThirdPerson = !Player.ThirdPerson;
        _snapCamera = true;
    }

    public bool Interact()
    {
        if (!Player.Driving)
        {
            if (!CanEnter()) return false;
            Tractor.Occupied = true;
            Player.SetDriving(true);
            _vehicleThird = true; _orbitYaw = 0; _orbitPitch = -.3f;
            _snapCamera = true;
            return true;
        }
        if (Mathf.Abs(Tractor.Speed) > .4f) { Message("Pare o trator antes de descer. Segure ESPAÇO para frear."); return false; }
        foreach (var local in new[] { new Vector3(2.1f, .12f, .5f), new Vector3(-2.1f, .12f, .5f), new Vector3(0, .12f, 3.1f) })
        {
            var candidate = Tractor.ToGlobal(local);
            var query = new PhysicsShapeQueryParameters3D {
                Shape = new CapsuleShape3D { Radius = .36f, Height = 1.8f },
                Transform = new Transform3D(Basis.Identity, candidate + Vector3.Up * .92f),
                CollisionMask = 1,
                Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() }
            };
            if (GetWorld3D().DirectSpaceState.IntersectShape(query).Count > 0) continue;
            Player.GlobalPosition = candidate;
            Player.Yaw = Tractor.Rotation.Y;
            Player.Pitch = -.08f;
            Player.SetDriving(false); Tractor.Occupied = false;
            _snapCamera = true;
            return true;
        }
        Message("Saída bloqueada. Leve o trator a um lugar mais aberto.");
        return false;
    }

    public bool CanEnter()
    {
        if (Player.GlobalPosition.DistanceTo(Tractor.GlobalPosition) > 3.7f) return false;
        var query = PhysicsRayQueryParameters3D.Create(Player.GlobalPosition + Vector3.Up * 1.4f, Tractor.GlobalPosition + Vector3.Up * 1.4f);
        query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 && hit["collider"].AsGodotObject() == Tractor;
    }

    public override void _Process(double delta)
    {
        if (_paused) return;
        UpdateCamera((float)delta);
        _status.Text = Player.Driving ? "NA DIREÇÃO  /  CBT 2105" : "EXPLORANDO  /  A PÉ";
        _speed.Text = Player.Driving ? $"{Mathf.Abs(Tractor.Speed) * 3.6f:00} km/h\n{(Tractor.Speed < -.1f ? "RÉ" : "FRENTE")}   •   FARÓIS {(Tractor.LightsOn ? "ACESOS" : "APAGADOS")}" : "MANHÃ NO CAMPO\nFazenda Vale das Flores";
        _hint.Text = Player.Driving ? "W / S  acelerar e ré     A / D  direção     ESPAÇO  freio\nE  descer     V  câmera     F  faróis     RODA DO MOUSE  distância" :
            CanEnter() ? "[ E ]  ENTRAR NO CBT 2105\nA câmera muda para terceira pessoa ao entrar" : "W A S D  caminhar     SHIFT  correr     ESPAÇO  pular\nV  primeira / terceira pessoa     E  entrar no trator";
        _messageTime -= (float)delta;
        _toast.Visible = _messageTime > 0;
    }

    private void UpdateCamera(float dt)
    {
        bool third = Player.Driving ? _vehicleThird : Player.ThirdPerson;
        float yaw = Player.Driving ? Tractor.Rotation.Y + _orbitYaw : Player.Yaw;
        float pitch = Player.Driving ? _orbitPitch : Player.Pitch;
        var basis = Basis.FromEuler(new(pitch, yaw, 0));
        Vector3 target = Player.Driving ? Tractor.Seat : Player.GlobalPosition + Vector3.Up * 1.67f;
        Tractor.Driver.Visible = Player.Driving && third;
        Vector3 position = target;
        if (third)
        {
            var desired = target + basis.Z * (Player.Driving ? _zoom : 4.2f);
            var query = PhysicsRayQueryParameters3D.Create(target, desired);
            query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid(), Tractor.GetRid() };
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            position = hit.Count > 0 ? hit["position"].AsVector3() + hit["normal"].AsVector3() * .28f : desired;
            position.Y = Mathf.Max(.25f, position.Y);
        }
        Camera.GlobalPosition = _snapCamera || !third ? position : Camera.GlobalPosition.Lerp(position, 1 - Mathf.Exp(-12 * dt));
        Camera.GlobalBasis = basis;
        _snapCamera = false;
    }

    private void Message(string text) { _toast.Text = text; _messageTime = 3; }

    public void SetPaused(bool pause)
    {
        _paused = pause; GetTree().Paused = pause; _pausePanel.Visible = pause;
        Input.MouseMode = pause ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && IsNodeReady()) SetPaused(true);
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
        var style = new StyleBoxFlat { BgColor = new Color(.09f, .14f, .1f, .88f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 14, ContentMarginBottom = 14 };
        panel.AddThemeStyleboxOverride("panel", style); parent.AddChild(panel); return panel;
    }

    private void CreateHud()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var title = Panel(root, new(28, 24), new(340, 104));
        var column = new VBoxContainer(); title.AddChild(column);
        Text(column, "VALE DAS FLORES", 28, new Color("f3e6bf"));
        _status = Text(column, "EXPLORANDO  /  A PÉ", 13, new Color("b9c6a6"));
        var info = Panel(root, Vector2.Zero, new(280, 95));
        info.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight); info.OffsetLeft = -308; info.OffsetRight = -28; info.OffsetTop = 24; info.OffsetBottom = 119;
        _speed = Text(info, "", 20, new Color("f3e6bf"));
        var hint = Panel(root, Vector2.Zero, new(760, 84));
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft); hint.OffsetLeft = 28; hint.OffsetRight = 788; hint.OffsetTop = -112; hint.OffsetBottom = -28;
        _hint = Text(hint, "", 17, new Color("f1e8ce"));
        var esc = Text(root, "ESC  pausa", 16, new Color("fff4d7"));
        esc.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight); esc.OffsetLeft = -130; esc.OffsetRight = -20; esc.OffsetTop = -50; esc.OffsetBottom = -20;
        var crosshair = Text(root, "+", 20, new Color(1, 1, 1, .7f));
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center); crosshair.OffsetLeft = -6; crosshair.OffsetTop = -14; crosshair.OffsetRight = 10; crosshair.OffsetBottom = 14;
        _toast = Text(root, "", 20, new Color("fff0b5"));
        _toast.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop); _toast.OffsetLeft = -330; _toast.OffsetTop = 150; _toast.OffsetRight = 430; _toast.OffsetBottom = 190;
        _pausePanel = new ColorRect { Color = new Color(.04f, .07f, .05f, .9f), Visible = false };
        root.AddChild(_pausePanel); _pausePanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer(); _pausePanel.AddChild(center); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var menu = new VBoxContainer { CustomMinimumSize = new(400, 0) }; center.AddChild(menu);
        menu.AddThemeConstantOverride("separation", 18);
        Text(menu, "UMA PAUSA NO CAMPO", 30, new Color("f3e6bf"));
        Text(menu, "Vale das Flores • protótipo", 18, new Color("bdcaa8"));
        var resume = new Button { Text = "Continuar  [ESC]", CustomMinimumSize = new(400, 52) }; menu.AddChild(resume); resume.Pressed += () => SetPaused(false);
        var quit = new Button { Text = "Sair do jogo", CustomMinimumSize = new(400, 52) }; menu.AddChild(quit); quit.Pressed += () => GetTree().Quit();
        Text(menu, "Exploração e condução. Sem salvamento nesta versão.", 14, new Color("bdcaa8"));
    }
}
