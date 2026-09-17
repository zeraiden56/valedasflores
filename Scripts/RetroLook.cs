using Godot;
using System.Collections.Generic;

namespace ValeDasFlores;

// One gentle pixel treatment covers the world and its console-style menus.
public partial class RetroLook : CanvasLayer
{
    private static RetroLook? _current;
    private readonly List<ColorRect> _screens = new();
    private ShaderMaterial _material = null!;
    private int _mode = 1;
    public string ModeName => _mode == 0 ? "NÍTIDO" : _mode == 1 ? "RETRÔ SUAVE" : "RETRÔ CLÁSSICO";
    public override void _Ready()
    {
        _current = this;
        Layer = 100; ProcessMode = ProcessModeEnum.Always;
        GetViewport().GuiEmbedSubwindows = true;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/retro_world.gdshader") };
        AddScreen(this);
        Apply();
    }
    private void AddScreen(Node parent)
    {
        var screen = new ColorRect { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = _mode != 0 };
        parent.AddChild(screen); screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _screens.Add(screen);
    }
    public static void DecoratePopup(Window popup)
    {
        if (_current == null || !GodotObject.IsInstanceValid(_current)) return;
        var layer = new CanvasLayer { Layer = 100, ProcessMode = ProcessModeEnum.Always };
        popup.AddChild(layer); _current.AddScreen(layer);
    }
    public override void _ExitTree()
    {
        if (_current == this) _current = null;
    }
    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.F8)
        {
            _mode = (_mode + 1) % 3; Apply();
            if (GetParent() is Farm farm) farm.Message("Visual: " + ModeName);
            GetViewport().SetInputAsHandled();
        }
    }
    private void Apply()
    {
        _screens.RemoveAll(screen => !GodotObject.IsInstanceValid(screen));
        foreach (var screen in _screens) screen.Visible = _mode != 0;
        _material.SetShaderParameter("pixel_size", _mode == 1 ? 2f : 3f);
        _material.SetShaderParameter("color_steps", _mode == 1 ? 64f : 32f);
    }
}
